#nullable enable
using System;
using Elffy.Threading;
using Elffy.Core;
using Elffy.AssemblyServices;

namespace Elffy
{
    /// <summary>Main engine class of Elffy</summary>
    public static class Engine
    {
        private static LazyApplyingList<IHostScreen> _screens = LazyApplyingList<IHostScreen>.New();

        /// <summary>Get whether the engine is running</summary>
        public static bool IsRunning { get; private set; }

        /// <summary>Get <see cref="IHostScreen"/> count which is running on the engine.</summary>
        public static int ScreenCount => _screens.Count;

        /// <summary>Get <see cref="IHostScreen"/> running on the engine.</summary>
        public static ReadOnlySpan<IHostScreen> Screens => _screens.AsSpan();

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
            screen.Dispose();
        }

        /// <summary>Start the engine</summary>
        public static void Run()
        {
            if(IsRunning) {
                ThrowAlreadyRunning();
                static void ThrowAlreadyRunning() => throw new InvalidOperationException("Engine is already runnning.");
            }
            IsRunning = true;
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
            if(AssemblyState.IsDebug) {
                // TODO: 暫定的実装
                GC.Collect();           // OpenGL 関連のメモリリーク検知用
            }
        }

        private static void ThrowNotRunning() => throw new InvalidOperationException("Engine is not running");
    }
}
