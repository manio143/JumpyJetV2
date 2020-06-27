// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Engine.Events;

namespace JumpyJetV2
{
    /// <summary>
    /// The script in charge of creating and updating the pipes.
    /// </summary>
    public class PipesScript : SyncScript
    {
        private const float GapBetweenPipe = 4f;
        private const float StartPipePosition = 4f;

        private EventReceiver diedListener = new EventReceiver(GlobalEvents.CharacterDied);
        private EventReceiver clearListener = new EventReceiver(GlobalEvents.Clear);
        private EventReceiver gameStartedListener = new EventReceiver(GlobalEvents.NewGame);
        private EventReceiver gameEndedListener = new EventReceiver(GlobalEvents.GameOver);

        private readonly List<Entity> pipeSets = new List<Entity>();
        
        private bool isScrolling;

        private Random random = new Random();

        private float sceneWidth;
        private float pipeOvervaluedWidth = 1f;

        [DataMemberIgnore]
        public int? RNGSeed;

        public UrlReference<Prefab> PipePrefabUrl { get; set; }

        public override void Start()
        {
            var pipeSetPrefab = Content.Load(PipePrefabUrl);

            // Create PipeSets
            sceneWidth = GameGlobals.GamePixelToUnitScale*GraphicsDevice.Presenter.BackBuffer.Width;
            var numberOfPipes = (int) Math.Ceiling(sceneWidth + 2* pipeOvervaluedWidth / GapBetweenPipe);
            for (int i = 0; i <= numberOfPipes; i++)
            {
                var pipeSet = pipeSetPrefab.Instantiate()[0];
                pipeSets.Add(pipeSet);
                Entity.AddChild(pipeSet);
            }

            // Reset the position of the PipeSets
            Reset();
        }

        public override void Update()
        {
            ProcessEvents();

            if (!isScrolling)
                return;

            var elapsedTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            bool pushBack = false;

            for (int i = 0; i < pipeSets.Count; i++)
            {
                var pipeSetTransform = pipeSets[i].Transform;

                // update the position of the pipe
                pipeSetTransform.Position -= new Vector3(elapsedTime * GameGlobals.PipeScrollSpeed, 0, 0);

                // move the pipe to the end of screen if not visible anymore
                if (pipeSetTransform.Position.X + pipeOvervaluedWidth / 2 < -sceneWidth / 2)
                {

                    // When a pipe is determined to be reset,
                    // get its next position by adding an offset to the position
                    // of a pipe which index is before itself.
                    var prevPipeSetIndex = (i + pipeSets.Count - 1) % pipeSets.Count;

                    var nextPosX = pipeSets[prevPipeSetIndex].Transform.Position.X + GapBetweenPipe;
                    pipeSetTransform.Position = new Vector3(nextPosX, GetPipeRandomYPosition(), 0);

                    pushBack = true; // push the pipeset to the end of the list
                }
            }

            if (pushBack)
            {
                var p = pipeSets[0];
                pipeSets.RemoveAt(0);
                pipeSets.Add(p);
            }
        }

        private void ProcessEvents()
        {
            if (gameEndedListener.TryReceive() || diedListener.TryReceive())
                isScrolling = false;

            if (clearListener.TryReceive())
                Reset();

            if (gameStartedListener.TryReceive())
                isScrolling = true;
        }

        private float GetPipeRandomYPosition()
        {
            return GameGlobals.GamePixelToUnitScale * random.Next(50, 225);
        }

        private void Reset()
        {
            if (RNGSeed.HasValue)
                random = new Random(RNGSeed.Value);
            for (var i = 0; i < pipeSets.Count; ++i)
                pipeSets[i].Transform.Position = new Vector3(StartPipePosition + i * GapBetweenPipe, GetPipeRandomYPosition(), 0);
        }

        public override void Cancel()
        {
            // remove all the children pipes.
            Entity.Transform.Children.Clear();
        }

        internal void ProvideAiInformation(ref float distanceToNext, ref float pipeHeight)
        {
            var i = 0;
            Entity pipeSet = pipeSets[i];
            while (pipeSet.Transform.Position.X < -1.7f) // -1.7 = -1.5 (half pipe) + -0.2 (player butt)
                pipeSet = pipeSets[++i];

            distanceToNext = pipeSet.Transform.Position.X;
            pipeHeight = pipeSet.Transform.Position.Y;
        }
    }
}
