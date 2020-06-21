using Stride.Engine;
using Stride.Core.Annotations;
using System.Diagnostics;

namespace JumpyJetV2
{
    public class InputController : SyncScript
    {
        public CharacterScript character;

        [NotNull]
        public IGameInput input;

        public override void Start()
        {
            if (input is AIInput ai)
                ai.Start();
        }

        public override void Update()
        {
            if (character != null && character.isRunning && !character.isDying)
            {
                GlobalEvents.CharacterUpdated.Broadcast(); // notify AI to finish previous frame
                if (input.Jumped)
                {
                    Debug.WriteLine("Jumped!");
                    character.Jump();
                }
            }
        }
    }
}
