#nullable enable
using System;
using Elffy.Threading;
using Elffy.Core;
using Elffy.AssemblyServices;

namespace Elffy
{
    public static class Engine
    {
        private static LazyApplyingList<IHostScreen> _screens = LazyApplyingList<IHostScreen>.New();

        public static bool IsRunning { get; private set; }

        public static int ScreenCount => _screens.Count;

        public static ReadOnlySpan<IHostScreen> Screens => _screens.AsSpan();

        public static void Start()
        {
            Run();
            try {
                while(HandleOnce()) ;
            }
            finally {
                Stop();
            }
        }

        internal static void AddScreen(IHostScreen screen, bool show = true)
        {
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

        public static void Run()
        {
            if(IsRunning) { throw new InvalidOperationException("Engine is already runnning."); }
            IsRunning = true;
        }

        public static bool HandleOnce()
        {
            _screens.ApplyAdd();
            foreach(var s in _screens.AsSpan()) {
                s.HandleOnce();
            }
            _screens.ApplyRemove();
            return _screens.Count != 0;
        }

        public static void Stop()
        {
            if(!IsRunning) { return; }
            IsRunning = false;
            if(AssemblyState.IsDebug) {
                // TODO: 暫定的実装
                GC.Collect();           // OpenGL 関連のメモリリーク検知用
            }
        }
    }
}
