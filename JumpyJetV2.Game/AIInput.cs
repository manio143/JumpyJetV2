using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
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

        private bool ShouldJump(in GameState gameState)
        {
            var t = 0.12f;
            var p = gameState.playerPos;
            var vNo = gameState.playerVel;
            var dist = gameState.pipeDistance;
            var height = gameState.pipeHeight;
            
            var jumpScore = ScorePath(t, p, CharacterMovement.JumpVelocity.Y, dist, height);
            var noJumpScore = ScorePath(t, p, vNo, dist, height);

            Debug.WriteLine((jumpScore, noJumpScore));

            return jumpScore > noJumpScore;
        }

        private const float CharacterBoxLength = 0.7f;
        private bool Dies(float pos, float v, float dist, float height)
        {
            var state = new GameState();
            state.playerPos = pos;
            state.playerVel = v;
            state.pipeDistance = dist;
            state.pipeHeight = height;
            return Dies(new[] { state });
        }
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

        private int ScorePath(float t, float pos, float v0, float dist, float height)
        {
            var (dies, (np, nv, nd)) = SimulateStep(t, pos, v0, dist, height);

            if (dies)
                return 0;
            if (nd < -3)
                return 1;

            var jumpScore = ScorePath(t, np, CharacterMovement.JumpVelocity.Y, nd, height);
            var noJumpScore = ScorePath(t, np, nv, nd, height);

            return jumpScore + noJumpScore;
        }

        private (bool, (float, float,float)) SimulateStep(float t, float pos, float v, float dist, float height)
        {
            var a = CharacterMovement.Gravity.Y;
            var pv = GameGlobals.PipeScrollSpeed;
            var pj = pos + v * t + (a * t * t) / 2;
            var pd = dist - pv * t;
            v += a * t;
            return (
                Dies(pj, v, pd, height),
                (pj, v, pd)
            );
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