// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Physics;
using Stride.Rendering.Sprites;

namespace JumpyJetV2
{
    /// <summary>
    /// CharacterScript is controlled by a user.
    /// The control is as follow, tapping a screen/clicking a mouse will make the agent jump up.
    /// </summary>
    public class CharacterScript : AsyncScript
    {
        private EventReceiver<GlobalEvents.PauseReason> gamePausedListener =
            new EventReceiver<GlobalEvents.PauseReason>(GlobalEvents.GamePaused);
        private EventReceiver<GlobalEvents.StartReason> gameStartedListener =
            new EventReceiver<GlobalEvents.StartReason>(GlobalEvents.GameStarted);

        private static readonly Vector2 Gravity = new Vector2(0, -17);
        private static readonly Vector2 StartPos = new Vector2(-1, 0);
        private static readonly Vector2 StartVelocity = new Vector2(0, 7);

        // magic number that matches the window resolution
        private const float TopLimit = (568 - 200) * GameGlobals.GamePixelToUnitScale;

        private const float NormalVelocityY = 6.5f;
        private const float DeathVelocityY = 1f;
        private const float VelocityAboveTopLimit = 2f;
        private const int FlyingSpriteFrameIndex = 1;
        private const int FallingSpriteFrameIndex = 0;
        private const int DeathSpriteFrameIndex = 3;

        private bool isRunning;
        private bool isDying;
        internal Vector2 velocity;
        internal Vector2 position;
        private float rotation;

        private SpriteFromSheet spriteProvider;
        private GameInput gameInput;

        public void Start()
        {
            spriteProvider = Entity.Get<SpriteComponent>().SpriteProvider as SpriteFromSheet;
            gameInput = Entity.Get<GameInput>();

            if (spriteProvider == null)
                throw new ArgumentNullException(nameof(spriteProvider), "This script requires a SpriteComponent.");
            if (gameInput == null)
                throw new ArgumentNullException(nameof(gameInput), "This script requires a GameInput component.");

            Reset();

            Script.AddTask(CountPassedPipes);
            Script.AddTask(DetectGameOver);
        }

        /// <summary>
        /// Reset CharacterScript parameters: position, velocity and set state.
        /// </summary>
        public void Reset()
        {
            position = StartPos;
            velocity = StartVelocity;
            rotation = 0;

            UpdateTransformation();

            isRunning = false;
            isDying = false;

            spriteProvider.CurrentFrame = FallingSpriteFrameIndex;
        }

        /// <summary>
        /// Update the agent according to its states: {Idle, Alive, Die}
        /// </summary>
        public async Task CountPassedPipes()
        {
            var physicsComponent = Entity.Components.Get<PhysicsComponent>();

            while (Game.IsRunning)
            {
                var collision = await physicsComponent.CollisionEnded();

                if (!isRunning || isDying)
                    continue;

                if (collision.ColliderA.CollisionGroup == CollisionFilterGroups.SensorTrigger ||
                    collision.ColliderB.CollisionGroup == CollisionFilterGroups.SensorTrigger)
                    GlobalEvents.PipePassed.Broadcast();
            }
        }

        /// <summary>
        /// Update the agent according to its states: {Idle, Alive, Die}
        /// </summary>
        public async Task DetectGameOver()
        {
            var physicsComponent = Entity.Components.Get<PhysicsComponent>();

            while (Game.IsRunning)
            {
                await Script.NextFrame();

                // detect collisions with the pipes
                var collision = await physicsComponent.NewCollision();
                if (collision.ColliderA.CollisionGroup == CollisionFilterGroups.DefaultFilter &&
                    collision.ColliderB.CollisionGroup == CollisionFilterGroups.DefaultFilter)
                {
                    GlobalEvents.GamePaused.Broadcast(GlobalEvents.PauseReason.Death);
                    await AnimateDeath();
                    GlobalEvents.GamePaused.Broadcast(GlobalEvents.PauseReason.GameOver);
                }
            }
        }

        private async Task AnimateDeath()
        {
            isDying = true;
            velocity.Y = DeathVelocityY;
            while (position.Y > -TopLimit * 1.2)
                await Script.NextFrame(); // wait for Jumpy to fall off screen
        }

        /// <summary>
        /// Update the agent according to its states: {Idle, Alive, Die}
        /// </summary>
        public override async Task Execute()
        {
            Start();

            while (Game.IsRunning)
            {
                await Script.NextFrame();

                ListenForPausedEvent();
                ListenForStartEvent();

                gameInput.Enabled = isRunning;

                if (!isRunning)
                    continue;

                var elapsedTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
                ProcessMovement(elapsedTime);

                // update animation and rotation value
                UpdateAgentAnimation();

                // update the position/rotation
                UpdateTransformation();
            }
        }

        private void ProcessMovement(float elapsedTime)
        {
            // apply impulse on the touch/space
            if (!isDying && gameInput.IsJumping)
                velocity.Y = position.Y > TopLimit ? VelocityAboveTopLimit : NormalVelocityY;

            // update position/velocity
            velocity += Gravity * elapsedTime;
            position += velocity * elapsedTime;
        }

        private void ListenForStartEvent()
        {
            if (gameStartedListener.TryReceive(out var startReason))
            {
                switch (startReason)
                {
                    case GlobalEvents.StartReason.NewGame:
                        Reset();
                        isRunning = true;
                        break;
                    case GlobalEvents.StartReason.Clear:
                        Reset();
                        break;
                    case GlobalEvents.StartReason.UnPause:
                        isRunning = true;
                        break;
                }
            }
        }

        private void ListenForPausedEvent()
        {
            if (gamePausedListener.TryReceive(out var pausedReason))
            {
                switch (pausedReason)
                {
                    case GlobalEvents.PauseReason.GameOver:
                        isRunning = false;
                        break;
                    case GlobalEvents.PauseReason.Pause:
                        isRunning = false;
                        break;
                }
            }
        }

        private void UpdateTransformation()
        {
            Entity.Transform.Position = new Vector3(position.X, position.Y, 0.1f);
            Entity.Transform.RotationEulerXYZ = new Vector3(0, 0, rotation);
        }

        private void UpdateAgentAnimation()
        {
            var isFalling = velocity.Y < 0;
            var rotationSign = isFalling ? -1 : 1;

            spriteProvider.CurrentFrame = 
                isDying ? DeathSpriteFrameIndex :
                isFalling ? FallingSpriteFrameIndex : FlyingSpriteFrameIndex;

            // Rotate a sprite
            rotation += rotationSign * MathUtil.Pi * 0.01f;
            if (rotationSign * rotation > Math.PI / 10f)
                rotation = rotationSign * MathUtil.Pi / 10f;
        }
    }
}
