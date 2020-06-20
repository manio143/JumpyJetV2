using Stride.Core;
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

        private const int StepSamples = 5;
        private const float StepSize = 0.16f;
        private bool ShouldJump(in GameState gameState)
        {
            (float posWithout, float posWith, float dist)[] future = new (float, float, float)[StepSamples];

            var vo = gameState.playerVel;
            var vw = CharacterMovement.JumpVelocity.Y;
            var pv = GameGlobals.PipeScrollSpeed;
            var a = CharacterMovement.Gravity.Y;
            var t = StepSize;

            future[0] = (
                gameState.playerPos + vo * t + (a * t * t) / 2,
                gameState.playerPos + vw * t + (a * t * t) / 2,
                gameState.pipeDistance + t * pv
            );

            for (int i = 1; i < StepSamples; i++)
            {
                vo += a * t;
                vw += a * t;
                future[i] = (
                    future[i - 1].posWithout + vo * t + (a * t * t) / 2,
                    future[i - 1].posWith + vw * t + (a * t * t) / 2,
                    future[i - 1].dist + t * pv
                );
            }

            var hl = gameState.pipeHeight - 4.4;
            var hu = gameState.pipeHeight + 2;
            //return future.Any(p => p.pos < h) && future.All(p => p.dist < 1.6f ? p.pos - h < 2 : true) ||
            //    future.Select(p => p.pos).Min() - h < 0.2f && gameState.pipeDistance < 1.6f;
            bool diesDown = false, diesUp = false;
            foreach (var (po, pw, pd) in future)
            {
                if (pd < -1.6)
                    continue;
                if (po < hl && pd < 1.75f && pd > -1.6f)
                    diesDown = true;
                if (pw > hu && pd < 1.75f && pd > -1.6f)
                    diesUp = true;
                if (po < hl - 4)
                    diesDown = true;
                if (po > hu + 5)
                    diesUp = true;
            }
            if (diesDown && !diesUp)
            {
                future.Select(x => { Debug.WriteLine(x); return false; }).ToArray();
                Debug.WriteLine("");
            }
            return diesDown && !diesUp; //TODO make this actually work :(
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