#nullable enable
using System;
using System.Collections.Generic;
using Elffy.UI;
using System.Drawing;
using Elffy.Threading;
using Elffy.Core.Timer;
using Elffy.Platforms;
using Elffy.Exceptions;
using Elffy.InputSystem;

namespace Elffy
{
    public static class Engine
    {
        public static bool IsRunning { get; private set; }

        public static IScreenHost CurrentScreen { get; private set; } = null!;

        public static void SingleScreenRun(int width, int height, string title, WindowStyle windowStyle, YAxisDirection uiYAxisDirection, ActionEventHandler<IScreenHost> initialized)
            => SingleScreenRun(width, height, title, windowStyle, uiYAxisDirection, null!, initialized);

        public static void SingleScreenRun(int width, int height, string title, WindowStyle windowStyle, 
                                           YAxisDirection uiYAxisDirection, string icon, ActionEventHandler<IScreenHost> initialized)
        {
            ArgumentChecker.ThrowOutOfRangeIf(width < 0, nameof(width), width, $"{nameof(width)} is out of range");
            ArgumentChecker.ThrowOutOfRangeIf(height < 0, nameof(height), height, $"{nameof(height)} is out of range");
            IsRunning = true;
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
                gameScreen.Dispatcher.SetMainThreadID();
                CurrentScreen = gameScreen;
                gameScreen.Show();
            }
            finally {
                CustomSynchronizationContext.Delete();
            }
        }

        private static IScreenHost GetWindowGameScreen(int width, int height, string title, WindowStyle windowStyle, YAxisDirection uiYAxisDirection, string? iconResourcePath)
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
}
