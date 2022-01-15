#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Cysharp.Threading.Tasks;
using Elffy;
using Elffy.Features;
using Elffy.Platforms;
using Elffy.Threading;

namespace UnitTest
{
    internal static class TestEngineEntryPoint
    {
        private static Func<IHostScreen, UniTask>? _func;

        public static void Start(Func<IHostScreen, UniTask> func)
        {
            ProcessHelper.SingleLaunch(() =>
            {
                _func = func;
                Launch();
            },
            () => throw new InvalidOperationException("Multiple engines cannot be started at the same time."));
        }

        private static void Launch()
        {
            EngineSetting.UserCodeExceptionCatchMode = UserCodeExceptionCatchMode.Throw;

            try {
                Elffy.Diagnostics.DevEnv.Run();
                Engine.Run();
                var screen = CreateScreen();
                screen.Initialized += OnScreenInitialized;
                CustomSynchronizationContext.Install(out var syncContextReciever);
                screen.Activate();
                while(Engine.HandleOnce()) {
                    syncContextReciever?.DoAll();
                }
            }
            finally {
                CustomSynchronizationContext.Restore();
                Engine.Stop();
                Elffy.Diagnostics.DevEnv.Stop();
            }
        }

        private static IHostScreen CreateScreen()
        {
            switch(Platform.PlatformType) {
                case PlatformType.Windows:
                case PlatformType.MacOSX:
                case PlatformType.Linux: {
                    return new Window(new WindowConfig
                    {
                        Width = 800,
                        Height = 600,
                        Title = "Test",
                        Style = WindowStyle.Default,
                        Visibility = WindowVisibility.Hidden,
                    });
                }
                case PlatformType.Android:
                case PlatformType.Other:
                default:
                    throw new PlatformNotSupportedException();
            }
        }

        [DebuggerHidden]
        private static async void OnScreenInitialized(IHostScreen screen)
        {
            //Timing.Initialize(screen);
            //Game.Initialize(screen);
            ExceptionDispatchInfo? edi = null;
            try {
                await (_func?.Invoke(screen) ?? throw new InvalidOperationException("func is null"));
            }
            catch(Exception ex) {
                edi = ExceptionDispatchInfo.Capture(ex);
                await screen.TimingPoints.Update.Next();
            }
            finally {
                screen.Close();
                //Timing.Clear();
                //Game.Clear();
                _func = null;
            }
            edi?.Throw();
        }
    }
}
