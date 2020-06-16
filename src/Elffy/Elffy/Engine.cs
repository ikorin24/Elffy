#nullable enable
using System;
using Elffy.UI;
using System.Drawing;
using Elffy.Threading;
using Elffy.Platforms;
using Elffy.Exceptions;
using Elffy.Platforms.Windows;
using Elffy.Effective.Internal;

namespace Elffy
{
    public static class Engine
    {
        public static bool IsRunning { get; private set; }

        public static void Run()
        {
            if(IsRunning) { throw new InvalidOperationException("Engine is already runnning."); }
            IsRunning = true;
            Dispatcher.SetMainThreadID();
            Resources.Initialize();
        }

        public static void ShowScreen(ActionEventHandler<IHostScreen> initialized)
            => ShowScreen(800, 450, "", initialized);

        public static void ShowScreen(int width, int height, string title, ActionEventHandler<IHostScreen> initialized)
            => ShowScreen(width, height, title, null, WindowStyle.Default, YAxisDirection.TopToBottom, initialized);

        public static void ShowScreen(int width, int height, string title, Icon? icon, WindowStyle windowStyle,
                                      YAxisDirection uiYAxisDirection, ActionEventHandler<IHostScreen> initialized)
        {
            ArgumentChecker.ThrowIfNullArg(initialized, nameof(initialized));
            if(!IsRunning) { throw new InvalidOperationException($"{nameof(Engine)} is not running."); }
            try {
                IHostScreen screen;
                switch(Platform.PlatformType) {
                    //case PlatformType.Windows: {
                    //    screen = new FormScreen(uiYAxisDirection);
                    //    break;
                    //}
                    case PlatformType.Windows:
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
                screen.Initialized += initialized;
                screen.Show(width, height, title, icon, windowStyle);
            }
            finally {
                CustomSynchronizationContext.Delete();
            }
        }
    }
}
