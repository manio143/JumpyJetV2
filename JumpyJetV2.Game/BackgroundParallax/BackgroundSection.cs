// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Graphics;
using System;

namespace JumpyJetV2
{
    /// <summary>
    /// A section of the parallax background.
    /// </summary>
    public class BackgroundSection
    {
        public enum Depth { Far = 0, Medium = 1, Close = 2, Foreground = 3 }
        public static readonly float[] DepthMapping = { 0f, 1f, 2f, 3f };

        private readonly Depth depth;

        public struct ScreenSetup
        {
            public Int2 Resolution;
            public Vector2 Center;
        }
        private readonly ScreenSetup screen;

        // Texture
        private Texture texture;
        private RectangleF textureRegion;

        private struct Quad
        {
            public Vector2 Position;
            public Vector2 Origin;
            public RectangleF Region;
        }

        private Quad fstQuad;
        private Quad sndQuad;

        public bool IsUpdating { get; set; } = true;
        public float ScrollPos { get; protected set; } = 0;
        public float ScrollWidth { get; protected set; } = 0;

        public BackgroundSection(Sprite backgroundSprite, ScreenSetup screen,
                                 Depth depth, Vector2 startPos = default)
        {
            this.depth = depth;
            this.screen = screen;

            fstQuad.Position = startPos;
            sndQuad.Position = startPos;

            CreateBackground(backgroundSprite.Texture, backgroundSprite.Region);
        }

        private static float ScrollSpeed(Depth depth)
        {
            switch (depth)
            {
                case Depth.Far: return GameGlobals.BackgroundScrollSpeed / 4f;
                case Depth.Medium: return GameGlobals.BackgroundScrollSpeed / 3f;
                case Depth.Close: return GameGlobals.BackgroundScrollSpeed / 1.5f;
                case Depth.Foreground: return GameGlobals.BackgroundScrollSpeed;
                default: throw new ArgumentException("Invalid value for Depth enum.", nameof(depth));
            }
        }

        public void Update(float elapsedTime)
        {
            if (IsUpdating)
                Scroll(elapsedTime);
        }

        private void Scroll(float elapsedTime)
        {
            // Update Scroll position
            if (ScrollPos > textureRegion.Width)
                ScrollPos = 0;

            ScrollPos += elapsedTime * ScrollSpeed(depth);

            UpdateSpriteQuads();
        }

        public void DrawSprite(SpriteBatch spriteBatch)
        {
            DrawQuad(fstQuad, spriteBatch);

            if (sndQuad.Region.Width > 0) // if visible on screen
                DrawQuad(sndQuad, spriteBatch);
        }
        
        private void DrawQuad(Quad quad, SpriteBatch spriteBatch)
        {
            var fDepth = DepthMapping[(int)depth];
            spriteBatch.Draw(texture, quad.Position + screen.Center, quad.Region, Color.White, 0f,
                quad.Origin, 1f, SpriteEffects.None, ImageOrientation.AsIs, fDepth);
        }

        private void CreateBackground(Texture bgTexture, RectangleF texReg)
        {
            texture = bgTexture;
            textureRegion = texReg;

            // Set offset to rectangle
            fstQuad.Region.X = textureRegion.X;
            fstQuad.Region.Y = textureRegion.Y;

            fstQuad.Region.Width = (textureRegion.Width > screen.Resolution.X) ? screen.Resolution.X : textureRegion.Width;
            fstQuad.Region.Height = (textureRegion.Height > screen.Resolution.Y) ? screen.Resolution.Y : textureRegion.Height;

            // Centering the content
            fstQuad.Origin.X = 0.5f * fstQuad.Region.Width;
            fstQuad.Origin.Y = 0.5f * fstQuad.Region.Height;

            // Copy data from first quad to second one
            sndQuad = fstQuad;
        }

        private void UpdateSpriteQuads()
        {
            // Update first Quad
            var firstQuadNewWidth = textureRegion.Width - ScrollPos;
            fstQuad.Region.Width = firstQuadNewWidth;
            // Update X position of the first Quad
            fstQuad.Region.X = ScrollPos;

            // Update second Quad
            // Calculate new X position and width of the second quad
            var secondQuadNewWidth = (ScrollPos + screen.Resolution.X) - textureRegion.Width;
            var secondQuadNewXPosition = (screen.Resolution.X / 2 - secondQuadNewWidth) + secondQuadNewWidth / 2;

            sndQuad.Region.Width = secondQuadNewWidth;
            sndQuad.Position.X = secondQuadNewXPosition;
            sndQuad.Origin.X = secondQuadNewWidth / 2f;
        }
    }
}
