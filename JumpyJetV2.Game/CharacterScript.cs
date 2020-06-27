// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Physics;
using DIExtensions;
using Stride.Core;
using Stride.Core.Mathematics;
using System;

namespace JumpyJetV2
{
    /// <summary>
    /// CharacterScript is controlled by a user.
    /// The control is as follow, tapping a screen/clicking a mouse will make the agent jump up.
    /// </summary>
    public class CharacterScript : AsyncScript
    {
        private EventReceiver clearListener = new EventReceiver(GlobalEvents.Clear);
        private EventReceiver newGameListener = new EventReceiver(GlobalEvents.NewGame);
        private EventReceiver gameOverListener = new EventReceiver(GlobalEvents.GameOver);

        [DataMemberIgnore]
        public bool isRunning;
        [DataMemberIgnore]
        public bool isDying;

        [EntityComponent]
        [DataMemberIgnore]
        public CharacterMovement Movement = null;

        [EntityComponent]
        private CharacterAnimation Animation = null;

        [EntityComponent]
        private RigidbodyComponent physicsComponent = null;

        public uint CharacterId;

        public void Start()
        {
            this.InjectEntityComponents();
            Movement.CharacterId = CharacterId;

            Reset();

            Script.AddTask(CountPassedPipes);
            Script.AddTask(DetectGameOver);
        }

        /// <summary>
        /// Reset CharacterScript parameters: position, velocity and set state.
        /// </summary>
        public void Reset()
        {
            Movement.Reset();
            Animation.Reset();

            isRunning = false;
            isDying = false;
        }

        /// <summary>
        /// Update the agent according to its states: {Idle, Alive, Die}
        /// </summary>
        public async Task CountPassedPipes()
        {
            while (Game.IsRunning)
            {
                var collision = await physicsComponent.CollisionEnded();

                if (!isRunning || isDying)
                    continue;

                if (collision.ColliderA.CollisionGroup == CollisionFilterGroups.SensorTrigger ||
                    collision.ColliderB.CollisionGroup == CollisionFilterGroups.SensorTrigger)
                    GlobalEvents.PipePassed.Broadcast(CharacterId);
            }
        }

        /// <summary>
        /// Update the agent according to its states: {Idle, Alive, Die}
        /// </summary>
        public async Task DetectGameOver()
        {
            while (Game.IsRunning)
            {
                // detect collisions with the pipes
                var collision = await physicsComponent.NewCollision();
                if (collision.ColliderA.CollisionGroup == CollisionFilterGroups.DefaultFilter &&
                    collision.ColliderB.CollisionGroup == CollisionFilterGroups.DefaultFilter)
                {
                    GlobalEvents.CharacterDied.Broadcast(CharacterId);
                    await AnimateDeath();
                }
            }
        }

        private async Task AnimateDeath()
        {
            isDying = true;
            Animation.isDying = true;
            Movement.DieJump();
            while (!Movement.IsOutOfBounds())
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

                DebugText.Print($"Position: {Entity.Transform.Position}\nVelocity: {physicsComponent.LinearVelocity}", new Int2(20, 300));
                
                ProcessEvents();

                if (isRunning)
                {
                    Movement.Enabled = true;
                    Animation.Enabled = true;
                }
                else
                {
                    Movement.Enabled = false;
                    Animation.Enabled = false;
                }
            }
        }

        private void ProcessEvents()
        {
            if (clearListener.TryReceive())
                Reset();
            if (newGameListener.TryReceive())
                isRunning = true;
            if (gameOverListener.TryReceive())
                isRunning = false;
        }

        public void Jump()
        {
            if (isRunning && !isDying)
                Movement.Jump();
        }
    }
}
