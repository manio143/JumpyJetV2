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
        public enum PauseReason : byte { GameOver, Pause, Death }
        public enum StartReason : byte { NewGame, UnPause, Clear }

        public static EventKey<PauseReason> GamePaused = new EventKey<PauseReason>("Global", "Game Paused");
        public static EventKey<StartReason> GameStarted = new EventKey<StartReason>("Global", "Game Started");
        public static EventKey PipePassed = new EventKey("Global", "Pipe Passed");
        public static EventKey CharacterUpdated = new EventKey("Global", "Character Updated");
    }
}
