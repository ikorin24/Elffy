#nullable enable
using System;
using Elffy.UI;
using System.Drawing;
using Elffy.Threading;
using Elffy.Platforms;
using Elffy.Exceptions;
using Elffy.Platforms.Windows;
using Elffy.Effective;

namespace Elffy
{
    public static class Engine
    {
        public static bool IsRunning { get; private set; }

        /// <summary>Get or set current <see cref="IHostScreen"/>. This is current target screen of rendering.</summary>
        public static IHostScreen CurrentScreen
        {
            get => _currentScreen ?? throw new InvalidOperationException($"There is no {nameof(IHostScreen)} instance in {nameof(Engine)}.");
            private set => _currentScreen = value;
        }
        private static IHostScreen? _currentScreen = null;

        public static void Run()
        {
            if(IsRunning) { throw new InvalidOperationException("Engine is already runnning."); }
            IsRunning = true;
            Dispatcher.SetMainThreadID();
            Resources.Initialize();
        }

        /// <summary>Switch current screen to specified screen. This method is for delegate</summary>
        /// <param name="dummy">dummy instance of extension method source. (null is valid because this is not used in this method.)</param>
        /// <param name="screen">screen of switching target</param>
        /// <remarks>
        /// This method is called every frame from delegate.
        /// Static method called from delegate is slower than instance one.
        /// If Currying by extension method, it is called as faster as method of instance.
        /// </remarks>
        internal static void SwitchScreen(this CurriedDelegateDummy? dummy, IHostScreen screen)
        {
            CurrentScreen = screen;
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
                    case PlatformType.Windows: {
                        screen = new FormScreen(uiYAxisDirection);
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
                screen.Show(width, height, title, icon, windowStyle);
            }
            finally {
                CustomSynchronizationContext.Delete();
            }
        }
    }
}
