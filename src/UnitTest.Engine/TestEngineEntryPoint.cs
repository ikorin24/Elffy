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

        public const string UseEngineSymbol = "UseEngine";

        public static void Start(Action<IHostScreen> action)
        {
            Start(screen =>
            {
                action.Invoke(screen);
                return UniTask.CompletedTask;
            });
        }

        public static void Start(Func<IHostScreen, UniTask> func)
        {
            Start(UserCodeExceptionCatchMode.Throw, func);
        }

        public static void Start(UserCodeExceptionCatchMode exceptionMode, Func<IHostScreen, UniTask> func)
        {
            ProcessHelper.SingleLaunch(() =>
            {
                _func = func;
                Launch(exceptionMode);
            },
            () => throw new InvalidOperationException("Multiple engines cannot be started at the same time."));
        }

        private static void Launch(UserCodeExceptionCatchMode exceptionMode)
        {
            EngineSetting.UserCodeExceptionCatchMode = exceptionMode;

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
                await screen.Timings.Update.Next();
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
