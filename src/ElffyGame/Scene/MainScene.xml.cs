using System;
using Elffy;

namespace ElffyGame.Scene
{
    public class MainScene : Elffy.Scene
    {
        public void OnLoaded()
        {
            DebugManager.Append("MainScene is Loaded");
        }
    }
}
