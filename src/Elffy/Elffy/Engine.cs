#nullable enable
using System;
using Elffy.UI;
using System.Drawing;
using Elffy.Threading;
using Elffy.Platforms;
using Elffy.Exceptions;
using Elffy.Platforms.Windows;

namespace Elffy
{
    public static class Engine
    {
        public static bool IsRunning { get; private set; }

        public static IHostScreen CurrentScreen { get; private set; } = null!;

        public static void SingleScreenRun(int width, int height, string title, WindowStyle windowStyle, YAxisDirection uiYAxisDirection, ActionEventHandler<IHostScreen> initialized)
            => SingleScreenRun(width, height, title, windowStyle, uiYAxisDirection, null!, initialized);

        public static void SingleScreenRun(int width, int height, string title, WindowStyle windowStyle, 
                                           YAxisDirection uiYAxisDirection, string icon, ActionEventHandler<IHostScreen> initialized)
        {
            ArgumentChecker.ThrowOutOfRangeIf(width < 0, nameof(width), width, $"{nameof(width)} is out of range");
            ArgumentChecker.ThrowOutOfRangeIf(height < 0, nameof(height), height, $"{nameof(height)} is out of range");
            if(IsRunning) { throw new InvalidOperationException("Engine is already runnning."); }
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
                Dispatcher.SetMainThreadID();
                CurrentScreen = gameScreen;
                gameScreen.Show();
            }
            finally {
                CustomSynchronizationContext.Delete();
            }
        }

        public static void Run()
        {
            if(IsRunning) { throw new InvalidOperationException("Engine is already runnning."); }
            IsRunning = true;
            Dispatcher.SetMainThreadID();
            Resources.Initialize();
        }

        internal static void SwitchScreen(IHostScreen screen)
        {
            CurrentScreen = screen;
        }

        public static void ShowScreen(int width, int height, string title, WindowStyle windowStyle, YAxisDirection uiYAxisDirection, 
                                      string? iconResourcePath, ActionEventHandler<IHostScreen> initialized)
        {
            ArgumentChecker.ThrowIfNullArg(initialized, nameof(initialized));
            if(!IsRunning) { throw new InvalidOperationException($"{nameof(Engine)} is not running."); }
            IHostScreen screen;
            switch(Platform.PlatformType) {
                case PlatformType.Windows: {
                    screen = new FormScreen(uiYAxisDirection);
                    screen.ClientSize = new Size(width, height);
                    // TODO: icon, window style
                    break;
                }
                case PlatformType.MacOSX:
                case PlatformType.Unix: {
                    screen = new Window(width, height, title, windowStyle, uiYAxisDirection);
                    break;
                }
                case PlatformType.Android:
                case PlatformType.Other:
                default:
                    throw Platform.PlatformNotSupported();
            }
            screen.TargetRenderPeriod = screen.FrameDelta.TotalSeconds; // TODO: ???
            screen.Initialized += initialized;
            screen.Show();
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
}
