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
using System.Diagnostics;

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
        public bool Broadcast = true;

        public void Start()
        {
            this.InjectEntityComponents();

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
                    if(Broadcast)
                        GlobalEvents.PipePassed.Broadcast();
            }
        }

        /// <summary>
        /// Update the agent according to its states: {Idle, Alive, Die}
        /// </summary>
        public async Task DetectGameOver()
        {
            while (Game.IsRunning)
            {
                // detect collisions with the pipes and floor
                var collision = await physicsComponent.NewCollision();

                if (!isRunning)
                    continue;

                if (collision.ColliderA.CollisionGroup == CollisionFilterGroups.DefaultFilter ||
                    collision.ColliderB.CollisionGroup == CollisionFilterGroups.DefaultFilter)
                {
                    if(Broadcast)
                        GlobalEvents.CharacterDied.Broadcast();
                    await AnimateDeath();
                    if (Broadcast)
                        GlobalEvents.GameOver.Broadcast();
                    isRunning = false;
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

                //DebugText.Print($"Position: {Entity.Transform.Position}\nVelocity: {physicsComponent.LinearVelocity}", new Int2(20, 300));
                
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
        }

        public void Jump()
        {
            if (isRunning && !isDying)
                Movement.Jump();
        }
    }
}
