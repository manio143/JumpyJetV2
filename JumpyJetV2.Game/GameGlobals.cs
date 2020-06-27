// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine.Events;

namespace JumpyJetV2
{
    static class GameGlobals
    {
        public const float PipeScrollSpeed = 2.90f;
        public const float BackgroundScrollSpeed = 100 * 2.90f;
        public const float GamePixelToUnitScale = 0.01f;
    }

    static class GlobalEvents
    {
        /// <summary>
        /// Signals the game ended.
        /// </summary>
        public static EventKey GameOver = new EventKey("Global", "Game Over");

        /// <summary>
        /// Signals the game state should be cleared.
        /// </summary>
        public static EventKey Clear = new EventKey("Global", "Clear Game State");

        /// <summary>
        /// Signals the start of a new game.
        /// </summary>
        public static EventKey NewGame = new EventKey("Global", "New Game");

        /// <summary>
        /// Signals the player *i* has passed a pipe.
        /// </summary>
        public static EventKey<uint> PipePassed = new EventKey<uint>("Global", "Pipe Passed");

        /// <summary>
        /// Signals the player *i* has died.
        /// </summary>
        public static EventKey<uint> CharacterDied = new EventKey<uint>("Global", "Character Died");

        /// <summary>
        /// Signals the player *i* was updated.
        /// </summary>
        public static EventKey<uint> CharacterUpdated = new EventKey<uint>("Global", "Character Updated");
    }
}
