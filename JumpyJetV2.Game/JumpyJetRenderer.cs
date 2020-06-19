// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace JumpyJetV2
{
    public class JumpyJetRenderer : SceneRendererBase
    {
        private SpriteBatch spriteBatch;

        private List<BackgroundSection> backgroundParallax;


        public SpriteSheet ParallaxBackgrounds { get; set; }

        /// <summary>
        /// The main render stage for opaque geometry.
        /// </summary>
        public RenderStage OpaqueRenderStage { get; set; }

        /// <summary>
        /// The transparent render stage for transparent geometry.
        /// </summary>
        public RenderStage TransparentRenderStage { get; set; }

        public void StartScrolling()
        {
            EnableAllParallaxesUpdate(true);
        }

        public void StopScrolling()
        {
            EnableAllParallaxesUpdate(false);
        }

        private void EnableAllParallaxesUpdate(bool isEnable)
        {
            foreach (var pallarax in backgroundParallax)
            {
                pallarax.IsUpdating = isEnable;
            }
        }

        protected override void InitializeCore()
        {
            AssertRequiredPropertiesAreProvided();

            base.InitializeCore();

            var bgSprites = ParallaxBackgrounds.Sprites;

            InitializeScreenSetup(out var screenSetup, out var virtualResolution);
            InitializeFloorPosition(bgSprites[3].SizeInPixels.Y, out var floorPosition);

            // Create Parallax Background
            backgroundParallax = new List<BackgroundSection>()
            {
                new BackgroundSection(bgSprites[0], screenSetup, BackgroundSection.Depth.Far),
                new BackgroundSection(bgSprites[1], screenSetup, BackgroundSection.Depth.Medium),
                new BackgroundSection(bgSprites[2], screenSetup, BackgroundSection.Depth.Close),
                new BackgroundSection(bgSprites[3], screenSetup, BackgroundSection.Depth.Foreground, floorPosition)
            };

            // allocate the sprite batch in charge of drawing the backgrounds.
            spriteBatch = new SpriteBatch(GraphicsDevice) { VirtualResolution = virtualResolution };
        }

        private void InitializeScreenSetup(out BackgroundSection.ScreenSetup screenSetup,
                                           out Vector3 virtualResolution)
        {
            virtualResolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height, 20f);
            screenSetup.Resolution = new Int2((int)virtualResolution.X, (int)virtualResolution.Y);
            screenSetup.Center = new Vector2(virtualResolution.X / 2, virtualResolution.Y / 2);
        }

        private void InitializeFloorPosition(float height, out Vector2 floorPosition)
        {
            // For Ground, move it downward so that its bottom edge is at the bottom screen.
            var screenHeight = GraphicsDevice.Presenter.BackBuffer.Height;
            floorPosition = Vector2.UnitY * (screenHeight - height) / 2;
        }

        private void AssertRequiredPropertiesAreProvided()
        {
            if (OpaqueRenderStage == null)
                throw new ArgumentNullException(nameof(OpaqueRenderStage), "Required field is null");
            if (TransparentRenderStage == null)
                throw new ArgumentNullException(nameof(TransparentRenderStage), "Required field is null");
        }

        protected override void CollectCore(RenderContext context)
        {
            // Setup pixel formats for RenderStage
            using (context.SaveRenderOutputAndRestore())
            {
                // Fill RenderStage formats and register render stages to main view
                context.RenderView.RenderStages.Add(OpaqueRenderStage);
                OpaqueRenderStage.Output = context.RenderOutput;

                context.RenderView.RenderStages.Add(TransparentRenderStage);
                TransparentRenderStage.Output = context.RenderOutput;
            }
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            var renderSystem = context.RenderSystem;

            // Clear
            drawContext.CommandList.Clear(drawContext.CommandList.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

            // Draw parallax background
            spriteBatch.Begin(drawContext.GraphicsContext);

            float elapsedTime = (float)context.Time.Elapsed.TotalSeconds;
            foreach (var pallaraxBackground in backgroundParallax)
            {
                pallaraxBackground.Update(elapsedTime);
                pallaraxBackground.DrawSprite(spriteBatch);
            }

            spriteBatch.End();

            // Draw [main view | main stage]
            renderSystem.Draw(drawContext, context.RenderView, OpaqueRenderStage);

            // Draw [main view | transparent stage]
            renderSystem.Draw(drawContext, context.RenderView, TransparentRenderStage);
        }
    }
}
