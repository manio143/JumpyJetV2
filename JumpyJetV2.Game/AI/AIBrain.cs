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
using Stride.Core.MicroThreading;
using Stride.Engine;
using Stride.Engine.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace JumpyJetV2.AI
{
    public class AIBrain : IAIBrain
    {
        private MultithreadSynchronization sync = new MultithreadSynchronization();

        public bool Train { get; set; }

        public UIScript UI { get; set; }
        public PipesScript Pipes { get; set; }

        public struct PlayerAndPipes
        {
            public Vector2 player;
            public Vector2 upper;
            public Vector2 lower;
        }
        public PlayerAndPipes playerAndPipes;

        /// <summary>
        /// Given a GameState the AI predicts wether the character should jump or not.
        /// </summary>
        /// <returns>should jump?</returns>
        public async Task<bool> Predict(AIInput.GameState gameState, uint id = 0)
        {
            // Debug data to draw helper lines
            playerAndPipes.player = new Vector2(-1, gameState.playerPos);
            playerAndPipes.upper = new Vector2(gameState.pipeDistance - 1, gameState.pipeHeight + 1.2f);
            playerAndPipes.lower = new Vector2(gameState.pipeDistance - 1, gameState.pipeHeight - 1.2f);

            double distanceToUpperPipe = playerAndPipes.upper.Y - playerAndPipes.player.Y;
            double distanceToLowerPipe = playerAndPipes.lower.Y - playerAndPipes.player.Y;
            double[] input = new double[] { gameState.playerPos, /*gameState.playerVel,*/ distanceToUpperPipe, distanceToLowerPipe, gameState.pipeDistance };

            if (Train)
            {
                sync.Input = input;
                sync.OnInput.Set();

                await sync.OnOutput.WaitAsync();
                HasPredicted = true;
                return sync.Output;
            }
            else
            {
                JumpyEvaluator.ProcessInputProduceOutput(trainedPhenome, input, out var output);
                return output;
            }
        }

        /// <summary>
        /// Return feedback to the AI so that it can learn.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public void Inform(AIInput.CharacterMoveResult result, uint id = 0)
        {
            switch (result)
            {
                case AIInput.CharacterMoveResult.Lived:
                    sync.Result = 1.0;
                    break;
                case AIInput.CharacterMoveResult.Died:
                    sync.Result = -1.0;
                    break;
                case AIInput.CharacterMoveResult.PipePassed:
                    sync.Result = 5.0;
                    break;
                default:
                    throw new NotImplementedException();
            }
            sync.OnResult.Set();
            HasPredicted = false;
        }

        public (uint generation, uint seqNum, uint highscore, uint genhigh) GetStats()
        {
            return (sync.Generation + 1, (uint)genomeListEvaluator.EvaluationCount % SpecieCount + 1, sync.HighScore, sync.GenHighScore);
        }

        private class MultithreadSynchronization
        {
            public uint Generation;
            public uint HighScore;
            public uint GenHighScore;

            public double[] Input;
            public AsyncAutoResetEvent OnInput = new AsyncAutoResetEvent();

            public bool Output;
            public AsyncAutoResetEvent OnOutput = new AsyncAutoResetEvent();

            public double Result;
            public AsyncAutoResetEvent OnResult = new AsyncAutoResetEvent();
        }

        private class JumpyEvaluator : IPhenomeEvaluator<IBlackBox>
        {
            public MultithreadSynchronization sync;
            public JumpyEvaluator(MultithreadSynchronization _sync) => sync = _sync;

            public ulong EvaluationCount { get; set; }

            public bool StopConditionSatisfied { get; set; }

            private const double JumpTreshold = 0.5;
            private const double FitnessTreshold = 1000;
            private const double PipesPassedTreshold = 50;
            public FitnessInfo Evaluate(IBlackBox phenome)
            {
                double fitness = 0.0;
                int pipesCrossed = 0;
                while (true)
                {
                    sync.OnInput.WaitAsync().Synchronously(); //check if this works

                    ProcessInputProduceOutput(phenome, sync.Input, out var output);
                    sync.Output = output;

                    sync.OnOutput.Set();

                    sync.OnResult.WaitAsync().Synchronously();

                    fitness += sync.Result;
                    if (sync.Result < 0)
                        break;
                    if (sync.Result > 1)
                        pipesCrossed++;

                    if (pipesCrossed > 5 * PipesPassedTreshold)
                        break;

                    sync.HighScore = (uint)Math.Max(sync.HighScore, pipesCrossed);
                    sync.GenHighScore = (uint)Math.Max(sync.GenHighScore, pipesCrossed);
                }

                if (pipesCrossed > PipesPassedTreshold)
                    StopConditionSatisfied = true;

                EvaluationCount++;

                return new FitnessInfo(fitness, fitness);
            }

            internal static void ProcessInputProduceOutput(IBlackBox phenome, double[] input, out bool output)
            {
                phenome.ResetState();

                phenome.InputSignalArray.CopyFrom(input, 0);

                phenome.Activate();

                var outVal = phenome.OutputSignalArray[0];

                var jumpValue = TanH.__DefaultInstance.Calculate(outVal, new double[0]);
                output = jumpValue > JumpTreshold;
            }

            public void Reset() { }
        }

        NeatEvolutionAlgorithm<NeatGenome> evolutionAlgorithm;
        NeatGenomeFactory neatGenomeFactory;
        List<NeatGenome> genomeList;
        SerialGenomeListEvaluator<NeatGenome, IBlackBox> genomeListEvaluator;
        private const int SpecieCount = 50;

        public bool HasPredicted { get; private set; }

        public void Start()
        {
            if (!Train)
            {
                Load();
                return;
            }

            UI.UserControlled = false;
            Pipes.RNGSeed = 0;

            neatGenomeFactory = new NeatGenomeFactory(inputNeuronCount: 4, outputNeuronCount: 1);

            TryLoadState(); // continue training

            var neatParameters = new NeatEvolutionAlgorithmParameters
            {
                SpecieCount = SpecieCount,
            };

            var distanceMetric = new ManhattanDistanceMetric();
            var speciationStrategy = new KMeansClusteringStrategy<NeatGenome>
                (distanceMetric);

            var complexityRegulationStrategy = new NullComplexityRegulationStrategy();

            var network = new NeatEvolutionAlgorithm<NeatGenome>
                (neatParameters, speciationStrategy, complexityRegulationStrategy);

            var activationScheme = NetworkActivationScheme
                .CreateCyclicFixedTimestepsScheme(1);
            var genomeDecoder = new NeatGenomeDecoder(activationScheme);

            var phenomeEvaluator = new JumpyEvaluator(sync);
            genomeListEvaluator =
                new SerialGenomeListEvaluator<NeatGenome, IBlackBox>
                    (genomeDecoder, phenomeEvaluator);

            network.UpdateScheme = new UpdateScheme(1);
            network.UpdateEvent += Network_UpdateEvent;

            evolutionAlgorithm = network;

            Task.Run(TrainAI);
        }

        private static readonly Random seedProvider = new Random();
        private void Network_UpdateEvent(object sender, EventArgs e)
        {
            sync.Generation++;
            sync.GenHighScore = 0;
            SaveState();
            //Pipes.RNGSeed = seedProvider.Next();
        }

        private async Task TrainAI()
        {
            evolutionAlgorithm.Initialize(genomeListEvaluator, neatGenomeFactory, genomeList);

            evolutionAlgorithm.StartContinue(); // execute network on a second thread

            while (evolutionAlgorithm.RunState == RunState.Running)
                await Task.Delay(5 * 1000); //check every 5 seconds if it stopped

            SaveBestAI();

            evolutionAlgorithm.Stop();
        }

        private const string AIFile = "ai_brain.xml";
        private void SaveBestAI()
        {
            var doc = NeatGenomeXmlIO.SaveComplete(new List<NeatGenome>() { evolutionAlgorithm.CurrentChampGenome }, false);
            doc.Save(AIFile);
        }

        private const string AITrainFile = "ai_brain_training_data.xml";
        internal void SaveState()
        {
            var doc = NeatGenomeXmlIO.SaveComplete(genomeList, false);
            doc.Save(AITrainFile);
        }
        internal void TryLoadState()
        {
            if (File.Exists(AITrainFile))
            {
                var xmlReader = XmlReader.Create(AITrainFile);
                genomeList = NeatGenomeXmlIO.ReadCompleteGenomeList(xmlReader, false, neatGenomeFactory);
                if (genomeList.Count != SpecieCount)
                    genomeList = neatGenomeFactory.CreateGenomeList(length: SpecieCount, 0);
            }
            else
            {
                genomeList = neatGenomeFactory.CreateGenomeList(length: SpecieCount, 0);
            }
        }

        private void Load()
        {
            UI.UserControlled = true;

            neatGenomeFactory = new NeatGenomeFactory(inputNeuronCount: 4, outputNeuronCount: 1);
            var activationScheme = NetworkActivationScheme
                .CreateCyclicFixedTimestepsScheme(1);
            var genomeDecoder = new NeatGenomeDecoder(activationScheme);

            var xmlReader = XmlReader.Create(AIFile);
            var genome = NeatGenomeXmlIO.ReadCompleteGenomeList(xmlReader, false, neatGenomeFactory)[0];

            trainedPhenome = genomeDecoder.Decode(genome);
        }
        private IBlackBox trainedPhenome;
    }

    public static class AsyncToSyncExtension
    {
        public static void Synchronously(this Task task)
            => task.ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
