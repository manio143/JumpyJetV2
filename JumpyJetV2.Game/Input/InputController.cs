using Stride.Engine;
using Stride.Core.Annotations;
using System.Diagnostics;
using System.Threading.Tasks;

namespace JumpyJetV2.Input
{
    public class InputController : AsyncScript
    {
        public CharacterScript character;

        [NotNull]
        public IGameInput input;

        internal void Start()
        {
            input.Initialize();
        }

        internal async Task Update()
        {
            if (character != null && character.isRunning && !character.isDying)
            {
                if (await input.HasJumped())
                {
                    Debug.WriteLine("Jumped!");
                    character.Jump();
                }
            }
        }

        public async override Task Execute()
        {
            Start();

            while (true)
            {
                await Script.NextFrame();
                await Update();
            }
        }
    }
}
