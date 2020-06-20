using Stride.Core;
using Stride.Engine;
using Stride.Input;
using System.Linq;

namespace JumpyJetV2
{
    [DataContract]
    public class PlayerInput : IGameInput
    {
        [DataMemberIgnore]
        public bool Jumped => controller.Input.IsKeyPressed(Keys.Space) || UserTappedScreen();

        public InputController controller;

        private bool UserTappedScreen()
        {
            return controller.Input.PointerEvents.Any(pointerEvent => pointerEvent.EventType == PointerEventType.Pressed);
        }
    }
}
