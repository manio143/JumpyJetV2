using Stride.Core;
using Stride.Core.Mathematics;
using System;
using System.Diagnostics;
using System.Linq;

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

        public CharacterScript characterScript;
        public PipesScript pipesScript;

        [DataMemberIgnore]
        public bool Jumped
        {
            get
            {
                FetchGameState(out var gameState);
                return ShouldJump(in gameState);
            }
        }

        private const float SimTime = 0.835f;
        private const int StepSamples = 10;
        private const float StepSize = SimTime/StepSamples;
        private bool ShouldJump(in GameState gameState)
        {
            var noJump = Simulate(in gameState, new int[] { });
            var jumpNow = Simulate(in gameState, new[] { 0 });
            var jumpNext = Simulate(in gameState, new[] { 1 });
            var noJumpNext = Simulate(in noJump[0], new int[] { });

            var noJumpDies = Dies(noJump);
            var jumpNowDies = Dies(jumpNow);
            var jumpNextDies = Dies(jumpNext);
            var noJumpNextDies = Dies(noJumpNext);

            Debug.WriteLine((noJumpDies, jumpNowDies, jumpNextDies, noJumpNextDies));

            if (jumpNowDies)
                return false;
            if (jumpNextDies && noJumpNextDies)
                return true;
            else
                return noJumpDies;
        }

        private const float CharacterBoxLength = 0.85f;
        private bool Dies(GameState[] states)
        {
            foreach(var state in states)
            {
                if (state.playerPos <= CharacterMovement.BottomLimit)
                    return true;
                var player = new RectangleF(-1 - CharacterBoxLength / 2,
                                                state.playerPos + CharacterBoxLength / 2,
                                                CharacterBoxLength,
                                                CharacterBoxLength);
                var lowerPipe = new RectangleF(state.pipeDistance - 1.5f,
                                               state.pipeHeight - 4.5f,
                                               3f,
                                               10f);
                var upperPipe = new RectangleF(state.pipeDistance - 1.5f,
                                               state.pipeHeight + 4.5f + 10f,
                                               3f,
                                               10f);
                if (player.Intersects(lowerPipe) || player.Intersects(upperPipe))
                    return true;
            }
            return false;
        }

        private GameState[] Simulate(in GameState initialState, int[] jumpAtSteps)
        {
            var v = initialState.playerVel;
            var s = initialState.playerPos;
            var ps = initialState.pipeDistance;
            var pv = GameGlobals.PipeScrollSpeed;
            var a = CharacterMovement.Gravity.Y;
            var t = StepSize;

            var simLength = jumpAtSteps.Length > 0 ? StepSamples : StepSamples / 2;
            GameState[] sim = new GameState[simLength];

            for (int i = 0; i < simLength; i++)
            {
                if(jumpAtSteps.Contains(i))
                    v = CharacterMovement.JumpVelocity.Y;
                
                s += v * t + (a * t * t) / 2;
                v += a * t;
                ps += pv * t;

                sim[i].playerPos = s;
                sim[i].playerVel = v;
                sim[i].pipeDistance = ps;
                sim[i].pipeHeight = initialState.pipeHeight;
            }

            return sim;
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