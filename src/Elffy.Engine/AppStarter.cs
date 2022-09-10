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
        private AppStarterConfig _config;
        private Action<IHostScreen>? _start;

        public AppStarterConfig Config { get => _config; set => _config = value; }


        private AppStarter(AppStarterConfig config)
        {
            _config = config;
        }

        public static AppStarter Create()
        {
            return new AppStarter(AppStarterConfig.Default);
        }

        public AppStarter WithConfig(AppStarterConfig config)
        {
            _config = config;
            return this;
        }

        public AppStarter WithTitle(string title)
        {
            _config = _config with { Title = title };
            return this;
        }

        public AppStarter WithIcon(ResourceFile icon)
        {
            _config = _config with { Icon = icon };
            return this;
        }

        public AppStarter WithWidth(int width)
        {
            _config = _config with { Width = width };
            return this;
        }

        public AppStarter WithHeight(int height)
        {
            _config = _config with { Height = height };
            return this;
        }

        public AppStarter WithStyle(WindowStyle style)
        {
            _config = _config with { Style = style };
            return this;
        }

        public AppStarter WithAllowMultiLaunch(bool allowMultiLaunch)
        {
            _config = _config with { AllowMultiLaunch = allowMultiLaunch };
            return this;
        }

        public AppStarter WithIsDebugMode(bool isDebugMode)
        {
            _config = _config with { IsDebugMode = isDebugMode };
            return this;
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
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. (Ignore exceptions in user code)
                }
            };

            if(_config.AllowMultiLaunch) {
                ProcessHelper.SingleLaunch(RunPrivate);
            }
            else {
                RunPrivate();
            }
        }

        private void RunPrivate()
        {
            try {
                if(_config.IsDebugMode) {
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
                if(_config.IsDebugMode) {
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
                    return new Window(_config.Width, _config.Height, _config.Title, _config.Style, _config.Icon);
                }
                case PlatformType.Android:
                case PlatformType.Other:
                default:
                    throw new PlatformNotSupportedException();
            }
        }
    }

    public record struct AppStarterConfig
    {
        public string Title { get; init; }
        public ResourceFile Icon { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public WindowStyle Style { get; init; }
        public bool AllowMultiLaunch { get; init; }
        public bool IsDebugMode { get; init; }

        public static AppStarterConfig Default => new()
        {
            Title = "App",
            Icon = ResourceFile.None,
            Width = 1200,
            Height = 675,
            Style = WindowStyle.Default,
            AllowMultiLaunch = false,
            IsDebugMode = false,
        };
    }
}
