using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using CircularBuffer;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;
using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers;
using ConvNetSharp.Core.Training;
using ConvNetSharp.Core.Serialization;
using Stride.Engine.Events;

namespace JumpyJetV2
{
    public class AIInput : GameInput
    {
        private EventReceiver<GlobalEvents.PauseReason> gamePausedListener =
            new EventReceiver<GlobalEvents.PauseReason>(GlobalEvents.GamePaused);
        private EventReceiver pipePassedListener = new EventReceiver(GlobalEvents.PipePassed);
        private bool jumpingButtonPressed;

        public override bool Enabled { get; set; }
        public override bool IsJumping => jumpingButtonPressed;

        private struct GameState
        {
            public float playerPos;
            public float playerVel;
            public float pipeHeight;
            public float pipeDistance;
        }

        private Brain brain;

        private CharacterScript characterScript;
        private PipesScript pipesScript;
        public override void Start()
        {
            characterScript = Entity.Get<CharacterScript>();
            pipesScript = Entity.Scene.Entities.FirstOrDefault(e => e.Name == "PipesScript")?.Get<PipesScript>();

            if (characterScript == null)
                throw new ArgumentNullException(nameof(characterScript), "This script requires a CharacterScript component.");
            if (pipesScript == null)
                throw new ApplicationException("Could not find enitity 'PipesScript' with PipesScript component.");

            brain = new Brain();
            brain.TryLoad();
        }

        bool firstPass = true;
        public override void Update()
        {
            if (pipePassedListener.TryReceive())
            {
                brain.Backward(5);
            }
            else if (gamePausedListener.TryReceive(out var reason)
                    && reason == GlobalEvents.PauseReason.GameOver)
            {
                FetchGameState(out var deathState);

                if (deathState.pipeDistance < -1)
                    brain.Backward(-1);
                else if (Math.Abs(deathState.playerPos - deathState.pipeHeight/2) < 2f)
                    brain.Backward(-5);
                else
                    brain.Backward(-10);
                
                GlobalEvents.GameStarted.Broadcast(GlobalEvents.StartReason.NewGame);
                firstPass = true;
                return;
            }
            else if (!firstPass)
            {
                brain.Backward(0);
            }

            firstPass = false;

            if (Enabled)
            {
                FetchGameState(out var state);
                var actionIdx = brain.Forward(state);
                jumpingButtonPressed = actionIdx == 1;

                if (brain.forwardPasses % 300 == 0)
                    brain.SaveState();

                if (Input.IsKeyPressed(Stride.Input.Keys.T))
                    brain.Train = !brain.Train;
            }
        }

        private void FetchGameState(out GameState state)
        {
            state = new GameState();
            state.playerPos = characterScript.position.Y;
            state.playerVel = characterScript.velocity.Y;
            pipesScript.ProvideAiInformation(ref state.pipeDistance, ref state.pipeHeight);
        }
        private class Brain
        {
            struct Experience
            {
                public Volume<float> state0;
                public int action0;
                public int reward0;
                public Volume<float> state1;
            }

            private const int numInputs = 4; // how many properties are given in GameState
            private const int numActions = 2; //jump or not
            private const int temporalWindow = 7; // must be at least 2
            private const int networkSize = numInputs * (1 + temporalWindow) + numActions * temporalWindow;
            private const int expSize = 5000;
            private const int learningThreshold = 200;
            private const float gamma = 0.5f;

            private Net<float> neuralNet;
            private TrainerBase<float> trainer;

            CircularBuffer<Volume<float>> netWindow = new CircularBuffer<Volume<float>>(temporalWindow);
            CircularBuffer<int> actionWindow = new CircularBuffer<int>(temporalWindow);
            CircularBuffer<int> rewardWindow = new CircularBuffer<int>(temporalWindow);
            CircularBuffer<GameState> stateWindow = new CircularBuffer<GameState>(temporalWindow);

            CircularBuffer<Experience> experiences = new CircularBuffer<Experience>(expSize);

            public bool Train { get; set; }

            public Brain()
            {
                neuralNet = new Net<float>();
                neuralNet.AddLayer(new InputLayer<float>(1, 1, networkSize));
                neuralNet.AddLayer(new FullyConnLayer<float>(50));
                neuralNet.AddLayer(new ReluLayer<float>());
                neuralNet.AddLayer(new FullyConnLayer<float>(50));
                neuralNet.AddLayer(new ReluLayer<float>());
                neuralNet.AddLayer(new FullyConnLayer<float>(numActions));
                neuralNet.AddLayer(new RegressionLayer<float>());

                trainer = new SgdTrainer<float>(neuralNet)
                {
                    LearningRate = 0.2f,
                    BatchSize = 128,
                };

                for(int i = 0; i < temporalWindow; i++)
                {
                    stateWindow.PushBack(new GameState());
                    actionWindow.PushBack(0);
                }
            }

            private static Random rnd = new Random();
            public int GetRandomAction()
            {
                if (rnd.NextDouble() > 0.8)
                    return 1; // jump
                return 0;
            }
            public static float[] StateToArray(in GameState state)
            {
                return new[] {
                    state.playerPos,
                    state.playerVel,
                    state.pipeDistance,
                    state.pipeHeight
                };

            }
            public Volume<float> PrepareNetworkInput(in GameState newState)
            {
                List<float> w = new List<float>();

                w.AddRange(StateToArray(newState));

                for (var k = 0; k < temporalWindow; k++)
                {
                    // state
                    w.AddRange(StateToArray(stateWindow[temporalWindow - 1 - k]));
                    // action, encoded as 1-of-k indicator vector. We scale it up a bit because
                    // we dont want weight regularization to undervalue this information, as it only exists once
                    var action1ofk = new float[numActions];
                    for (var q = 0; q < numActions; q++)
                        action1ofk[q] = 0;
                    action1ofk[actionWindow[temporalWindow - 1 - k]] = 1 * numInputs;
                    w.AddRange(action1ofk);
                }

                return BuilderInstance.Volume.From(w.ToArray(), new Shape(networkSize));
            }

            public (int action, float value) Policy(Volume<float> inputState)
            {
                Volume<float> actionValues = neuralNet.Forward(inputState, false);
                int bestAction = 0;
                float bestValue = actionValues.Get(0);

                for (int i = 1; i < numActions; i++)
                    if (actionValues.Get(i) > bestValue)
                    {
                        bestAction = i;
                        bestValue = actionValues.Get(i);
                    }

                return (bestAction, bestValue);
                }

            public int forwardPasses { get; private set; }
            double epsilon = 0.3;
            const double epsilon_decr_rate = 0.97;
            const double epsilon_min = 0.05;
            public int Forward(in GameState state)
            {
                int action;
                var stateVol = PrepareNetworkInput(state);
                if (forwardPasses > temporalWindow)
                {
                    if(rnd.NextDouble() < epsilon)
                    {
                        action = GetRandomAction();
                    }
                    else
                    {
                        var (act, _) = Policy(stateVol);
                        action = act;
                    }
                }
                else
                {
                    action = GetRandomAction();
                }

                stateWindow.PushBack(state);
                actionWindow.PushBack(action);
                netWindow.PushBack(stateVol);

                forwardPasses++;

                return action;
            }

            public void Backward(int reward)
            {
                reward *= 10; // reward is more imporant than neural network's action value
                rewardWindow.PushBack(reward);

                if (forwardPasses > temporalWindow + 1)
                {
                    var exp = new Experience();
                    exp.state0 = netWindow[temporalWindow - 2]; // one to last state
                    exp.action0 = actionWindow[temporalWindow - 2];
                    exp.reward0 = rewardWindow[temporalWindow - 2];
                    exp.state1 = netWindow[temporalWindow - 1]; // last state

                    this.experiences.PushBack(exp);
                }

                var experiences = this.experiences.Where(e => e.reward0 != 0).ToList();

                if(Train && experiences.Count > learningThreshold)
                {
                    for (var k = 0; k < trainer.BatchSize; k++)
                    {
                        var re = rnd.Next(experiences.Count);
                        var e = experiences[re];
                        var q_eval = neuralNet.Forward(e.state0, false).ToArray();
                        var q_next = neuralNet.Forward(e.state1, false).ToArray();

                        var q_target = q_eval;
                        q_target[e.action0] = e.reward0 + gamma * q_next.Max();

                        var y = BuilderInstance.Volume.From(q_target, new Shape(q_target.Length));
                        trainer.Train(e.state0, y);
                    }
                    epsilon *= epsilon_decr_rate;
                    if (epsilon < epsilon_min) epsilon = epsilon_min;
                }
            }

            private const string brainDump = "JumpyBrain.json";
            public void SaveState()
            {
                string net = neuralNet.ToJson();
                File.WriteAllText(brainDump, net);
            }
            public void TryLoad()
            {
                if(File.Exists(brainDump))
                {
                    string net = File.ReadAllText(brainDump);
                    neuralNet = SerializationExtensions.FromJson<float>(net);
                }
            }
        }
    }

}
