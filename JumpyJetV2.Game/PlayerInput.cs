using Stride.Core;
using Stride.Input;
using System.Linq;
using System.Threading.Tasks;

namespace JumpyJetV2
{
    [DataContract]
    public class PlayerInput : IGameInput
    {
        public InputController controller;

        private bool UserTappedScreen()
        {
            return controller.Input.PointerEvents.Any(pointerEvent => pointerEvent.EventType == PointerEventType.Pressed);
        }

        public void Initialize() { }

        public Task<bool> HasJumped()
        {
            var jumped = controller.Input.IsKeyPressed(Keys.Space) || UserTappedScreen();
            return Task.FromResult(jumped);
        }
    }
}
