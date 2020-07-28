#nullable enable
using System;
using Elffy.UI;
using System.Drawing;
using Elffy.Threading;
using Elffy.Platforms;
using Elffy.Exceptions;
using Elffy.Platforms.Windows;
using Elffy.Effective.Unsafes;
using Elffy.Core;
using System.Runtime.CompilerServices;
using Elffy.OpenGL;
using Elffy.AssemblyServices;

namespace Elffy
{
    public static class Engine
    {
        private static DefaultGLResource? _glResource;

        public static TextureObject WhiteEmptyTexture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _glResource?.WhiteEmptyTexture
                ?? throw new InvalidOperationException("Engine is already runnning.");
        }

        public static bool IsRunning { get; private set; }

        public static void Run()
        {
            if(IsRunning) { throw new InvalidOperationException("Engine is already runnning."); }
            IsRunning = true;
            Dispatcher.SetMainThreadID();
        }

        public static void End()
        {
            if(!IsRunning) { return; }
            IsRunning = false;
            if(_glResource is null == false) {
                _glResource.Dispose();
                _glResource = null;
            }
            if(AssemblyState.IsDebug) {
                // TODO: 暫定的実装
                GC.Collect();           // OpenGL 関連のメモリリーク検知用
            }
        }

        public static void ShowScreen(ActionEventHandler<IHostScreen> initialized)
            => ShowScreen(800, 450, "", initialized);

        public static void ShowScreen(int width, int height, string title, ActionEventHandler<IHostScreen> initialized)
            => ShowScreen(width, height, title, null, WindowStyle.Default, initialized);

        public static void ShowScreen(int width, int height, string title, Icon? icon, WindowStyle windowStyle,
                                      ActionEventHandler<IHostScreen> initialized)
        {
            if(initialized is null) { throw new ArgumentNullException(nameof(initialized)); }
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
                        screen = new Window(width, height, title, windowStyle);
                        break;
                    }
                    case PlatformType.Android:
                    case PlatformType.Other:
                    default:
                        throw Platform.PlatformNotSupported();
                }
                var glResource = new DefaultGLResource();
                glResource.Create();
                _glResource = glResource;
                screen.Initialized += initialized;
                screen.Show(width, height, title, icon, windowStyle);
            }
            finally {
                CustomSynchronizationContext.Delete();
            }
        }
    }
}
