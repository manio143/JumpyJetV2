// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Rendering.Compositing;

namespace JumpyJetV2
{
    public class BackgroundScript : AsyncScript
    {
        public override async Task Execute()
        {
            // Find our JumpyJetRenderer to start/stop parallax background
            var renderer = (JumpyJetRenderer)((SceneCameraRenderer)SceneSystem.GraphicsCompositor.Game).Child;

            var pauseListener = new EventReceiver(GlobalEvents.CharacterDied);
            var startListener = new EventReceiver(GlobalEvents.Clear);

            while (Game.IsRunning)
            {
                // we don't care for the reason - either way we stop the background
                await pauseListener.ReceiveAsync();
                renderer.StopScrolling();
                startListener.Reset(); // remove any start events that happened before pause

                await startListener.ReceiveAsync();
                renderer.StartScrolling();
                pauseListener.Reset(); // remove any pause events that happened before the start
            }
        }
    }
}
