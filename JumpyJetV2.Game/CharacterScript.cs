// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Physics;
using DIExtensions;
using Stride.Core;
using Stride.Core.Mathematics;

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

                ListenForPausedEvent();
                ListenForStartEvent();

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

        public void Jump()
        {
            if (isRunning && !isDying)
                Movement.Jump();
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
    }
}
