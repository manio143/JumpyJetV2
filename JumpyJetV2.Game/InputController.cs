using Stride.Engine;
using Stride.Core.Annotations;

namespace JumpyJetV2
{
    public class InputController : SyncScript
    {
        public CharacterScript character;

        [NotNull]
        public IGameInput input;

        public override void Update()
        {
            if (character != null && character.isRunning)
            {
                if (input.Jumped)
                    character.Jump();
            }
        }
    }
}
