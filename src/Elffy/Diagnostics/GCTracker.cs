#nullable enable
using System.Diagnostics;
using Elffy.Effective;

namespace Elffy.Diagnostics
{
    internal static class GCTracker
    {
        private static bool _isRunning;

        public static void Init()
        {
            if(_isRunning) { return; }
            _isRunning = true;

            GCCallback.Register(OnGC0, 0);
            GCCallback.Register(OnGC1, 1);
            GCCallback.Register(OnGC2, 2);
        }

        private static bool OnGC0()
        {
            if(!_isRunning) { return false; }

            Debug.WriteLine("----- GC gen 0 -----");
            return true;
        }

        private static bool OnGC1()
        {
            if(!_isRunning) { return false; }

            Debug.WriteLine("----- GC gen 1 -----");
            return true;
        }

        private static bool OnGC2()
        {
            if(!_isRunning) { return false; }

            Debug.WriteLine("----- GC gen 2 -----");
            return true;
        }

        public static void End()
        {
            _isRunning = false;
        }
    }
}
