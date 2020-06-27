using JumpyJetV2.Input;
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

namespace JumpyJetV2.AI
{
    [DataContract]
    public class AIInput : IGameInput
    {
        public struct GameState
        {
            public float playerPos;
            public float playerVel;
            public float pipeHeight;
            public float pipeDistance;
        }
        public enum CharacterMoveResult : byte
        {
            Lived,
            Died,
            PipePassed,
        }

        public CharacterScript characterScript;
        public PipesScript pipesScript;
        public AIFeedback feedback;

        private AIBrain brain = new AIBrain();

        public void Initialize()
        {
            feedback.Initialize(brain); // feedback sets Train on brain
            brain.Pipes = pipesScript;
            brain.Start();
        }

        public Task<bool> HasJumped()
        {
            FetchGameState(out var gameState);
            return brain.Predict(gameState);
        }

        private void FetchGameState(out GameState state)
        {
            state = new GameState();
            var position = characterScript.Movement.Position;
            var velocity = characterScript.Movement.Velocity;
            state.playerPos = position.Y;
            state.playerVel = velocity.Y;
            pipesScript.ProvideAiInformation(ref state.pipeDistance, ref state.pipeHeight);

            state.pipeDistance -= position.X;
        }
    }
}