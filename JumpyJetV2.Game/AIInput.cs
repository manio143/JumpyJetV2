using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JumpyJetV2
{
    [DataContract]
    public class AIInput : IGameInput
    {
        private struct GameState
        {
            public float playerPos;
            public float playerVel;
            public float pipeHeight;
            public float pipeDistance;
        }

        private const double JumpTreshold = 0.5;
        private const double FitnessTreshold = 1000;

        public CharacterScript characterScript;
        public PipesScript pipesScript;

        private AutoResetEvent inputProvider = new AutoResetEvent(false);
        private AutoResetEvent outputProvider = new AutoResetEvent(false);

        private class JumpyEvaluator : IPhenomeEvaluator<IBlackBox>
        {
            private readonly AIInput input;
            private EventReceiver<GlobalEvents.PauseReason> gamePausedListener =
                new EventReceiver<GlobalEvents.PauseReason>(GlobalEvents.GamePaused);
            private EventReceiver<GlobalEvents.StartReason> gameStartedListener =
                new EventReceiver<GlobalEvents.StartReason>(GlobalEvents.GameStarted);
            private EventReceiver characterUpdated = new EventReceiver(GlobalEvents.CharacterUpdated);
            public JumpyEvaluator(AIInput _input) => input = _input;

            public ulong EvaluationCount => 0;

            public bool StopConditionSatisfied => shouldEnd;
            bool shouldEnd = false;
            public FitnessInfo Evaluate(IBlackBox phenome)
            {
                // wait for game to start
                gameStartedListener.ReceiveAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                double fitness = 0.0;
                while (true)
                {
                    if (!input.inputProvider.WaitOne(300))
                        break;

                    input.FetchGameState(out var gameState);

                    phenome.ResetState();

                    phenome.InputSignalArray[0] = gameState.playerPos;
                    phenome.InputSignalArray[1] = gameState.playerVel;
                    phenome.InputSignalArray[2] = gameState.pipeDistance;
                    phenome.InputSignalArray[3] = gameState.pipeHeight;

                    phenome.Activate();

                    var output = phenome.OutputSignalArray[0];

                    var jumpValue = TanH.__DefaultInstance.Calculate(output, new double[0]);
                    input.shouldJump = jumpValue > JumpTreshold;

                    characterUpdated.Reset();
                    input.outputProvider.Set();

                    // wait for physics to be updated
                    characterUpdated.ReceiveAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                    if(gamePausedListener.TryReceive(out var pauseReason))
                    {
                        if(pauseReason == GlobalEvents.PauseReason.Death)
                            break; // end loop
                    }
                    else
                    {
                        // jumpy survived
                        fitness += 1;
                    }
                }

                if (fitness > FitnessTreshold)
                    shouldEnd = true;

                return new FitnessInfo(fitness, fitness);
            }

            public void Reset()
            {
                gamePausedListener.Reset();
                gameStartedListener.Reset();
            }
        }

        NeatEvolutionAlgorithm<NeatGenome> evolutionAlgorithm;

        public void Start()
        {
            var neatGenomeFactory = new NeatGenomeFactory(inputNeuronCount: 4, outputNeuronCount: 1);
            var genomeList = neatGenomeFactory.CreateGenomeList(length: 100, 0);
            var neatParameters = new NeatEvolutionAlgorithmParameters
            {
                SpecieCount = 100
            };

            var distanceMetric = new EuclideanDistanceMetric();
            var speciationStrategy = new ParallelKMeansClusteringStrategy<NeatGenome>
                (distanceMetric);

            var complexityRegulationStrategy = new NullComplexityRegulationStrategy();

            var network = new NeatEvolutionAlgorithm<NeatGenome>
                (neatParameters, speciationStrategy, complexityRegulationStrategy);

            var activationScheme = NetworkActivationScheme
                .CreateCyclicFixedTimestepsScheme(1);
            var genomeDecoder = new NeatGenomeDecoder(activationScheme);

            var phenomeEvaluator = new JumpyEvaluator(this);
            var genomeListEvaluator =
                new SerialGenomeListEvaluator<NeatGenome, IBlackBox>
                    (genomeDecoder, phenomeEvaluator);

            evolutionAlgorithm = network;

            Task.Run(() =>
            {
                network.Initialize(genomeListEvaluator, neatGenomeFactory, genomeList);
                network.StartContinue(); // execute network on a second thread
            });
        }

        private bool shouldJump;

        [DataMemberIgnore]
        public bool Jumped
        {
            get
            {
                inputProvider.Set();
                outputProvider.WaitOne();
                return shouldJump;
            }
        }

        private void FetchGameState(out GameState state)
        {
            state = new GameState();
            var position = characterScript.Movement.Position;
            var velocity = characterScript.Movement.Velocity;
            state.playerPos = position.Y;
            state.playerVel = velocity.Y;
            pipesScript.ProvideAiInformation(ref state.pipeDistance, ref state.pipeHeight);
        }
    }
}