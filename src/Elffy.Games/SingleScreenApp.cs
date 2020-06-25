#nullable enable
using Elffy.Core;
using Elffy.InputSystem;
using Elffy.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elffy.Games
{
    public static class SingleScreenApp
    {
        private static Action _initialize = null!;
        private static IHostScreen _screen = null!;
        private static Layer _worldLayer = null!;
        private static Camera _mainCamera = null!;
        private static Mouse _mouse = null!;

        public static IHostScreen Screen => _screen;
        public static Layer WorldLayer => _worldLayer;

        public static Camera MainCamera => _mainCamera;

        public static Mouse Mouse => _mouse;

        public static void Start(Action initialize)
        {
            _initialize = initialize ?? throw new ArgumentNullException(nameof(initialize));
            ProcessHelper.SingleLaunch(StartPrivate);
        }

        private static void StartPrivate()
        {
            try {
                Resources.Initialize();
                Engine.Run();
                Engine.ShowScreen(1600, 900, "Game", Resources.LoadIcon("icon.ico"), WindowStyle.Default, YAxisDirection.TopToBottom, InitScreen);
            }
            finally {
                Engine.End();
            }
        }

        private static void InitScreen(IHostScreen screen)
        {
            _screen = screen;
            _worldLayer = screen.Layers.WorldLayer;
            _mainCamera = screen.Camera;
            _mouse = screen.Mouse;
            _initialize();
        }
    }
}
