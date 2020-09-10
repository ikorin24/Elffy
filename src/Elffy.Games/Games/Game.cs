#nullable enable
using Elffy.Core;
using Elffy.Diagnostics;
using Elffy.InputSystem;
using Elffy.UI;
using System;

namespace Elffy.Games
{
    public static class Game
    {
        private static Action _initialize = null!;
        private static IHostScreen _screen = null!;
        private static Layer _worldLayer = null!;
        private static Camera _mainCamera = null!;
        private static Mouse _mouse = null!;
        private static ControlCollection _uiRootCollection = null!;

        public static IHostScreen Screen => _screen;
        public static Layer WorldLayer => _worldLayer;

        public static Camera MainCamera => _mainCamera;

        public static Mouse Mouse => _mouse;

        public static ControlCollection UI => _uiRootCollection;

        public static void Start(int width, int height, string title, Action initialize)
            => Start(width, height, title, false, initialize);

        public static void Start(int width, int height, string title, bool isDebug, Action initialize)
        {
            _initialize = initialize ?? throw new ArgumentNullException(nameof(initialize));

            ProcessHelper.SingleLaunch(Launch);

            //void Launch() {
            //    try {
            //        if(isDebug) {
            //            DiagnosticsSetting.Run();
            //        }
            //        Resources.Initialize();
            //        Engine.Run();
            //        Engine.ShowScreen(width, height, title, Resources.LoadIcon("icon.ico"), WindowStyle.Default, InitScreen);   // TODO: リソース以外からのアイコン指定方法
            //    }
            //    finally {
            //        Engine.End();
            //        DiagnosticsSetting.Stop();
            //    }
            //}

            void Launch()
            {
                try {
                    if(isDebug) {
                        DiagnosticsSetting.Run();
                    }
                    var window = new Window();
                    window.Initialized += InitScreen;
                    Engine.Start(window);
                }
                finally {
                    if(isDebug) {
                        DiagnosticsSetting.Stop();
                    }
                }
            }
        }

        private static void InitScreen(IHostScreen screen)
        {
            _screen = screen;
            _worldLayer = screen.Layers.WorldLayer;
            _mainCamera = screen.Camera;
            _mouse = screen.Mouse;
            _uiRootCollection = screen.UIRoot.Children;
            _initialize();
        }
    }
}
