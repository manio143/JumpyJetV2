using Stride.Input;
using System.Linq;

namespace JumpyJetV2
{
    public class PlayerInput : GameInput
    {
        private bool jumpingButtonPressed;

        public override bool Enabled { get; set ; }
        public override bool IsJumping => jumpingButtonPressed;

        public override void Update()
        {
            if (Enabled)
            {
                if (Input.IsKeyPressed(Keys.Space) || UserTappedScreen())
                    jumpingButtonPressed = true;
                else
                    jumpingButtonPressed = false;
            }
        }

        private bool UserTappedScreen()
        {
            return Input.PointerEvents.Any(pointerEvent => pointerEvent.EventType == PointerEventType.Pressed);
        }
    }
}
