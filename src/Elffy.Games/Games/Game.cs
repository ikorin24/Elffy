#nullable enable
using Elffy.Core;
using Elffy.Diagnostics;
using Elffy.InputSystem;
using Elffy.Platforms;
using Elffy.Threading;
using Elffy.UI;
using System;

namespace Elffy.Games
{
    public static class Game
    {
        private static SyncContextReceiver? _syncContextReciever;
        private static Action _initialize = null!;

        public static IHostScreen Screen { get; private set; } = null!;
        public static Layer WorldLayer { get; private set; } = null!;
        public static Camera MainCamera { get; private set; } = null!;
        public static Mouse Mouse { get; private set; } = null!;
        public static ControlCollection UI { get; private set; } = null!;

        public static void Start(int width, int height, string title, Action initialize) => Start(width, height, title, false, initialize);

        public static void Start(int width, int height, string title, bool isDebug, Action initialize)
        {
            _initialize = initialize ?? throw new ArgumentNullException(nameof(initialize));
            ProcessHelper.SingleLaunch(Launch);

            void Launch()
            {
                var screen = CreateScreen(width, height, title);
                screen.Initialized += InitScreen;

                if(isDebug) {
                    DiagnosticsSetting.Run();
                }
                Engine.Run();
                try {
                    _syncContextReciever = new SyncContextReceiver();
                    CustomSynchronizationContext.Create(_syncContextReciever);
                    screen.Show();

                    while(Engine.HandleOnce()) {
                        _syncContextReciever.DoAll();
                    }
                }
                finally {
                    CustomSynchronizationContext.Delete();
                    _syncContextReciever = null;
                    Engine.Stop();

                    if(isDebug) {
                        DiagnosticsSetting.Stop();
                    }
                }
            }
        }

        private static void InitScreen(IHostScreen screen)
        {
            Screen = screen;
            WorldLayer = screen.Layers.WorldLayer;
            MainCamera = screen.Camera;
            Mouse = screen.Mouse;
            UI = screen.UIRoot.Children;
            _initialize();
        }

        private static IHostScreen CreateScreen(int width, int height, string title)
        {
            switch(Platform.PlatformType) {
                case PlatformType.Windows:
                case PlatformType.MacOSX:
                case PlatformType.Unix:
                    return new Window(width, height, title, WindowStyle.Default);
                case PlatformType.Android:
                case PlatformType.Other:
                default:
                    throw new PlatformNotSupportedException();
            }
        }
    }
}
