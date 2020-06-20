using Stride.Core;

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
            return false; //TODO: figure out the math for this AI
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