#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Features;
using Elffy.Platforms;
using Elffy.Threading;
using Elffy.Diagnostics;
using System;

namespace Elffy
{
    public sealed class AppStarter
    {
        public string Title { get; init; } = "App";
        public ResourceFile Icon { get; init; }
        public int Width { get; init; } = 800;
        public int Height { get; init; } = 450;
        public WindowStyle Style { get; init; } = WindowStyle.Default;
        public bool AllowMultiLaunch { get; init; } = false;
        public bool IsDebugMode { get; init; } = false;

        private Action<IHostScreen>? _start;

        public AppStarter()
        {
        }

        public void Run(Func<IHostScreen, UniTask> start)
        {
            ArgumentNullException.ThrowIfNull(start);
            _start = async screen =>
            {
                try {
                    await start(screen);
                }
                catch {
                    // Don't throw. (Ignore exceptions in user code)
                }
            };

            if(AllowMultiLaunch) {
                ProcessHelper.SingleLaunch(RunPrivate);
            }
            else {
                RunPrivate();
            }
        }

        private void RunPrivate()
        {
            try {
                if(IsDebugMode) {
                    DevEnv.Run();
                }
                Engine.Run();
                var screen = CreateScreen();
                screen.Initialized += _start;
                CustomSynchronizationContext.InstallIfNeeded(out _, out var syncContextReciever);
                screen.Activate();
                while(Engine.HandleOnce()) {
                    syncContextReciever?.DoAll();
                }
            }
            finally {
                CustomSynchronizationContext.Restore();
                Engine.Stop();
                if(IsDebugMode) {
                    DevEnv.Stop();
                }
            }
        }

        private IHostScreen CreateScreen()
        {
            switch(Platform.PlatformType) {
                case PlatformType.Windows:
                case PlatformType.MacOSX:
                case PlatformType.Linux: {
                    return new Window(Width, Height, Title, Style, Icon);
                }
                case PlatformType.Android:
                case PlatformType.Other:
                default:
                    throw new PlatformNotSupportedException();
            }
        }
    }
}
