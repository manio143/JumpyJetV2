using Stride.Engine;
using DIExtensions;
using Stride.Rendering.Sprites;
using System;
using Stride.Core.Mathematics;
using Stride.Core;

namespace JumpyJetV2
{
    public class CharacterAnimation : SyncScript
    {
        private const int FlyingSpriteFrameIndex = 1;
        private const int FallingSpriteFrameIndex = 0;
        private const int DeathSpriteFrameIndex = 3;

        private float rotation;

        [DataMemberIgnore]
        public bool isDying;

        public bool Enabled;

        [EntityComponent]
        private CharacterMovement Movement = null;

        [EntityComponent]
        private SpriteComponent spriteComponent = null;
        private SpriteFromSheet spriteProvider;

        public override void Start()
        {
            this.InjectEntityComponents();
            spriteProvider = spriteComponent.SpriteProvider as SpriteFromSheet;
            Reset();
        }

        public void Reset()
        {
            rotation = 0;
            isDying = false;
            
            if (spriteProvider != null) //allow calling reset before Start()
                spriteProvider.CurrentFrame = FallingSpriteFrameIndex;
        }

        public override void Update()
        {
            if (!Enabled)
                return;

            var isFalling = Movement.Velocity.Y < 0;
            var rotationSign = isFalling ? -1 : 1;

            spriteProvider.CurrentFrame =
                isDying ? DeathSpriteFrameIndex :
                isFalling ? FallingSpriteFrameIndex : FlyingSpriteFrameIndex;

            // Rotate a sprite
            rotation += rotationSign * MathUtil.Pi * 0.01f;
            if (rotationSign * rotation > Math.PI / 10f)
                rotation = rotationSign * MathUtil.Pi / 10f;

            Entity.Transform.RotationEulerXYZ = new Vector3(0, 0, rotation);
        }
    }
}
