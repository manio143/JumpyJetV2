using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Graphics.GeometricPrimitives;
using Stride.Particles.DebugDraw;
using System;
using System.Diagnostics;
using System.Linq;

namespace JumpyJetV2
{
    public class AIFeedback : SyncScript
    {
        private EventReceiver<GlobalEvents.PauseReason> pauseReceiver =
            new EventReceiver<GlobalEvents.PauseReason>(GlobalEvents.GamePaused);
        private EventReceiver pipeReceiver = new EventReceiver(GlobalEvents.PipePassed);

        private AIBrain brain;
        public bool Train { get; set; }
        
        public UIScript ui;

        public override void Update()
        {
            if (brain == null)
                return;

            if (Train)
            {
                var (gen, eval, high, genhigh) = brain.GetStats();
                DebugText.Print($"Generation: {gen}\nEvalCount: {eval}\nGen Highscore: {genhigh}\nHighscore: {high}", new Int2(20, 235));
            }

            if (brain.HasPredicted)
            {
                // draw lines
                DrawLines();

                if (pauseReceiver.TryReceive(out var reason) && reason == GlobalEvents.PauseReason.Death)
                    brain.Inform(AIInput.CharacterMoveResult.Died);
                else
                {
                    if (pipeReceiver.TryReceive())
                        brain.Inform(AIInput.CharacterMoveResult.PipePassed);
                    else
                        brain.Inform(AIInput.CharacterMoveResult.Lived);
                }
            }

            if (Input.IsKeyPressed(Stride.Input.Keys.S))
                brain.SaveState();
        }

        [Conditional("DEBUG")]
        private void DrawLines()
        {
            // THIS IS A HACK
            // I don't know an easier way to draw a line than to take a square texture
            // rescale it, rotate it and position it in such a way that it looks like a line
            if(lineU == null || lineL == null)
            {
                lineU = Entity.Scene.Entities.First(e => e.Name == "LineUpper");
                lineL = Entity.Scene.Entities.First(e => e.Name == "LineLower");
            }

            var data = brain.playerAndPipes;
            var upperVec = (data.upper - data.player);
            var lowerVec = (data.lower - data.player);

            // make it as long as the distance from player to pipe
            // horizontally
            lineU.Transform.Scale = new Vector3(upperVec.Length()/lineRectSide, 1, 1);
            lineL.Transform.Scale = new Vector3(lowerVec.Length()/lineRectSide, 1, 1);

            // given a 2D vector we can find an angle
            var thetaU = (float)Math.Atan2(upperVec.Y, upperVec.X); // radians
            lineU.Transform.RotationEulerXYZ = new Vector3(0, 0, thetaU);
            var thetaL = (float)Math.Atan2(lowerVec.Y, lowerVec.X); // radians
            lineL.Transform.RotationEulerXYZ = new Vector3(0, 0, thetaL);

            // Now the position is in the middle of the line
            lineU.Transform.Position = (Vector3)data.player;
            lineL.Transform.Position = (Vector3)data.player;

            // I HOPE THIS WORKS!!
        }
        private Entity lineU;
        private Entity lineL;
        private const float lineRectSide = 30 * GameGlobals.GamePixelToUnitScale;

        public void Initialize(AIBrain brain)
        {
            this.brain = brain;
            brain.Train = this.Train;
            brain.UI = this.ui;
            pauseReceiver.Reset();
        }
    }
}
