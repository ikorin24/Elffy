using System;
using Elffy;

namespace ElffyGame.Scene
{
    public class MainScene : GameScene
    {
        public void OnLoaded()
        {
            DebugManager.Append("MainScene is Loaded");
        }
    }
}
