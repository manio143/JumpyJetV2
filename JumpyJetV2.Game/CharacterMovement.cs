using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace JumpyJetV2
{
    public class CharacterMovement : SyncScript
    {
        internal static readonly Vector2 Gravity = new Vector2(0, -17);
        private static readonly Vector2 StartPos = new Vector2(-1, 0);
        private static readonly Vector2 StartVelocity = new Vector2(0, 7);
        internal static readonly Vector2 JumpVelocity = new Vector2(0, 6.5f);
        private static readonly Vector2 JumpAboveLimitVelocity = new Vector2(0, 2);
        private static readonly Vector2 DeathVelocity = new Vector2(0, 1);
        
        // magic number that matches the window resolution
        internal const float TopLimit = (568 - 200) * GameGlobals.GamePixelToUnitScale;
        internal const float BottomLimit = -TopLimit * 1.2f;

        [DataMemberIgnore]
        public Vector2 Velocity;
        [DataMemberIgnore]
        public Vector2 Position;

        public bool Enabled;
        public uint CharacterId;

        public void Reset()
        {
            Velocity = StartVelocity;
            Position = StartPos;

            Entity.Transform.Position = (Vector3)Position;
        }

        public void Jump()
        {
            if (Position.Y >= TopLimit)
                Velocity = JumpAboveLimitVelocity;
            else
                Velocity = JumpVelocity;
        }

        public void DieJump()
        {
            Velocity = DeathVelocity;
        }

        public override void Update()
        {
            if (!Enabled)
                return;

            float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            var v = Velocity;
            var t = deltaTime;
            var a = Gravity;

            if (!IsOutOfBounds())
            {
                Position += v * t + (a * t * t) / 2;
                Velocity += a * t;
            }

            Entity.Transform.Position = (Vector3)Position;

            GlobalEvents.CharacterUpdated.Broadcast(CharacterId);
        }

        public bool IsOutOfBounds()
        {
            return Position.Y <= BottomLimit;
        }
    }
}
