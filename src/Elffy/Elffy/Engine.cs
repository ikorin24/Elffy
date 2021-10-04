#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Threading;
using Elffy.Features.Internal;

namespace Elffy
{
    /// <summary>Main engine class of Elffy</summary>
    public static class Engine
    {
        [ThreadStatic]
        private static bool _isThreadMain;
        [ThreadStatic]
        private static IHostScreen? _currentContext;

        private static int _mainThreadID = 0;      // 0 means the engine is not running.
        private static readonly object _syncLock = new object();
        private static bool _isHandling;
        private static LazyApplyingList<IHostScreen> _screens = LazyApplyingList<IHostScreen>.New();
        private static readonly Stopwatch _watch = new Stopwatch();

        /// <summary>Get whether the current thread is the main thread of the engine or not.</summary>
        public static bool IsThreadMain => _isThreadMain;

        /// <summary>Get whether the engine is running</summary>
        public static bool IsRunning => _mainThreadID != 0;

        /// <summary>Get <see cref="IHostScreen"/> count which is running on the engine.</summary>
        public static int ScreenCount => _screens.Count;

        /// <summary>Get <see cref="IHostScreen"/> running on the engine.</summary>
        public static ReadOnlySpan<IHostScreen> Screens => _screens.AsSpan();

        /// <summary>Get current context screen. It may be null even if in the main thread.</summary>
        /// <remarks>Always returns null if not main thread.</remarks>
        public static IHostScreen? CurrentContext => _currentContext;

        /// <summary>Get real time since the engine started.</summary>
        public static TimeSpan RunningRealTime => _watch.Elapsed;

        internal static void AddScreen(IHostScreen screen)
        {
            if(!IsRunning) {
                ThrowInvalidOperation("The engine is not running.");
            }
            _screens.Add(screen);
        }

        internal static void RemoveScreen(IHostScreen screen)
        {
            if(!IsRunning) {
                ThrowInvalidOperation("The engine is not running.");
            }
            _screens.Remove(screen);
        }

        internal static void SetCurrentContext(IHostScreen? screen)
        {
            _currentContext = screen;
        }

        /// <summary>Start the engine</summary>
        public static void Run()
        {
            lock(_syncLock) {
                if(IsRunning) { ThrowInvalidOperation("The engine is already running."); }
                _watch.Restart();
                EngineSetting.Lock();
                _isThreadMain = true;
                Volatile.Write(ref _mainThreadID, Environment.CurrentManagedThreadId);  // This means 'IsRunning = true'
            }
        }

        /// <summary>Handle next frame of all screens the engine has.</summary>
        /// <returns>whether the engine requires to handle next frame. (Returns false if <see cref="ScreenCount"/> == 0)</returns>
        public static bool HandleOnce()
        {
            if(!IsRunning) { ThrowInvalidOperation("The engine is not running."); }
            if(!_isThreadMain) { ThrowInvalidOperation("Current thread is not main thread."); }

            // Only a main thread can enter here.

            if(_isHandling) { ThrowInvalidOperation($"{nameof(HandleOnce)} method is not re-entrant."); }
            _isHandling = true;
            try {
                _screens.ApplyAdd();
                foreach(var s in _screens.AsSpan()) {
                    s.HandleOnce();
                }
                _screens.ApplyRemove();
                return _screens.Count != 0;
            }
            finally {
                _isHandling = false;
            }
        }

        /// <summary>Stop the engine</summary>
        public static void Stop()
        {
            lock(_syncLock) {
                if(!IsRunning) { return; }
                if(!_isThreadMain) { ThrowInvalidOperation("Current thread is not main thread."); }

                // Only a main thread can enter here.

                _isThreadMain = false;
                _watch.Stop();
                //_watch.Reset();
                //EngineSetting.Unlock();
                Volatile.Write(ref _mainThreadID, 0);   // This means 'IsRunning = false'
            }
        }

        [DoesNotReturn]
        private static void ThrowInvalidOperation(string message) => throw new InvalidOperationException(message);
    }
}
