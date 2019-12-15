#nullable enable
using System;
using Elffy.UI;
using System.Drawing;
using Elffy.Threading;
using Elffy.Platforms;
using Elffy.Exceptions;

namespace Elffy
{
    public static class Engine
    {
        public static bool IsRunning => Mode != EngineRunningMode.NotRunning;
        public static EngineRunningMode Mode { get; private set; } = EngineRunningMode.NotRunning;

        public static IHostScreen CurrentScreen { get; private set; } = null!;

        public static void SingleScreenRun(int width, int height, string title, WindowStyle windowStyle, YAxisDirection uiYAxisDirection, ActionEventHandler<IHostScreen> initialized)
            => SingleScreenRun(width, height, title, windowStyle, uiYAxisDirection, null!, initialized);

        public static void SingleScreenRun(int width, int height, string title, WindowStyle windowStyle, 
                                           YAxisDirection uiYAxisDirection, string icon, ActionEventHandler<IHostScreen> initialized)
        {
            ArgumentChecker.ThrowOutOfRangeIf(width < 0, nameof(width), width, $"{nameof(width)} is out of range");
            ArgumentChecker.ThrowOutOfRangeIf(height < 0, nameof(height), height, $"{nameof(height)} is out of range");
            if(IsRunning) { throw new InvalidOperationException("Engine is already runnning."); }
            Mode = EngineRunningMode.SingleScreen;
            Resources.Initialize();
            try {
                var gameScreen = Platform.PlatformType switch
                {
                    PlatformType.Windows => GetWindowGameScreen(width, height, title, windowStyle, uiYAxisDirection, icon),
                    PlatformType.MacOSX => GetWindowGameScreen(width, height, title, windowStyle, uiYAxisDirection, icon),
                    PlatformType.Unix => GetWindowGameScreen(width, height, title, windowStyle, uiYAxisDirection, icon),
                    _ => throw Platform.PlatformNotSupported()
                };
                gameScreen.TargetRenderPeriod = gameScreen.FrameDelta.TotalSeconds; // TODO: ???
                gameScreen.Initialized += initialized;
                Dispatcher.SetMainThreadID();
                CurrentScreen = gameScreen;
                gameScreen.Show();
            }
            finally {
                CustomSynchronizationContext.Delete();
            }
        }

        public static void RunAsMultiScreenBackend()
        {
            if(IsRunning) { throw new InvalidOperationException("Engine is already runnning."); }
            Mode = EngineRunningMode.MultiScreen;
            Dispatcher.SetMainThreadID();
            Resources.Initialize();
        }

        public static void SwitchScreen(IHostScreen screen)
        {
            CurrentScreen = screen;
        }

        private static IHostScreen GetWindowGameScreen(int width, int height, string title, WindowStyle windowStyle, YAxisDirection uiYAxisDirection, string? iconResourcePath)
        {
            var window = new Window(width, height, title, windowStyle, uiYAxisDirection);
            if(iconResourcePath != null) {
                if(Resources.HasResource(iconResourcePath)) {
                    using(var stream = Resources.GetStream(iconResourcePath)) {
                        window.Icon = new Icon(stream);
                    }
                }
            }
            return window;
        }
    }

    public enum EngineRunningMode
    {
        NotRunning,
        SingleScreen,
        MultiScreen,
    }
}
