#nullable enable
using System;
using Elffy.Core;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

namespace Elffy
{
    /// <summary>Main engine class of Elffy</summary>
    public static class Engine
    {
        [ThreadStatic]
        private static IHostScreen? _currentContext;
        private static LazyApplyingList<IHostScreen> _screens = LazyApplyingList<IHostScreen>.New();
        private static readonly Stopwatch _watch = new Stopwatch();

        /// <summary>Get whether the engine is running</summary>
        public static bool IsRunning { get; private set; }

        /// <summary>Get <see cref="IHostScreen"/> count which is running on the engine.</summary>
        public static int ScreenCount => _screens.Count;

        /// <summary>Get <see cref="IHostScreen"/> running on the engine.</summary>
        public static ReadOnlySpan<IHostScreen> Screens => _screens.AsSpan();

        public static IHostScreen? CurrentContext => _currentContext;

        /// <summary>Get real time since the engine started.</summary>
        public static TimeSpan RunningRealTime => _watch.Elapsed;

        internal static void AddScreen(IHostScreen screen, bool show = true)
        {
            if(!IsRunning) {
                ThrowNotRunning();
            }

            _screens.Add(screen);
            if(show) {
                screen.Show();
            }
        }

        internal static void RemoveScreen(IHostScreen screen)
        {
            _screens.Remove(screen);
        }

        internal static void SetCurrentContext(IHostScreen? screen)
        {
            _currentContext = screen;
        }

        /// <summary>Start the engine</summary>
        public static void Run()
        {
            if(IsRunning) {
                ThrowAlreadyRunning();
            }
            IsRunning = true;
            _watch.Restart();
            EngineSettings.Lock();
        }

        /// <summary>Handle next frame of all screens the engine has.</summary>
        /// <returns>whether the engine requires to handle next frame. (Returns false if <see cref="ScreenCount"/> == 0)</returns>
        public static bool HandleOnce()
        {
            if(!IsRunning) {
                ThrowNotRunning();
            }

            _screens.ApplyAdd();
            foreach(var s in _screens.AsSpan()) {
                s.HandleOnce();
            }
            _screens.ApplyRemove();
            return _screens.Count != 0;
        }

        /// <summary>Stop the engine</summary>
        public static void Stop()
        {
            if(!IsRunning) { return; }
            IsRunning = false;
            _watch.Stop();
            _watch.Reset();
            EngineSettings.Unlock();
        }

        [DoesNotReturn]
        private static void ThrowNotRunning() => throw new InvalidOperationException("Engine is not running");

        [DoesNotReturn]
        private static void ThrowAlreadyRunning() => throw new InvalidOperationException("Engine is already runnning.");
    }
}
