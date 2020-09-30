#nullable enable
using System;
using System.Diagnostics;
using Elffy.AssemblyServices;
using Elffy.Effective;

namespace Elffy.Diagnostics
{
    internal static class GCTracker
    {
        private static bool _isRunning;
        private static readonly int[] _gcCount = new int[GC.MaxGeneration + 1];

        [Conditional(AssemblyState.Symbol_Develop)]
        public static void Init()
        {
            if(_isRunning) { return; }
            _isRunning = true;

            for(int i = 0; i < _gcCount.Length; i++) {
                _gcCount[i] = GC.CollectionCount(i);
            }

            GCCallback.Register(OnGCCollected, 0);
        }

        private static bool OnGCCollected()
        {
            if(!_isRunning) { return false; }

            var gen = 0;
            for(int i = 0; i < _gcCount.Length; i++) {
                var count = GC.CollectionCount(i);
                if(count != _gcCount[i]) {
                    gen = i;
                    _gcCount[i] = count;
                }
            }
            DevEnv.WriteLine($"----- GC gen {gen} -----");
            return true;
        }

        [Conditional(AssemblyState.Symbol_Develop)]
        public static void End()
        {
            _isRunning = false;
            _gcCount.AsSpan().Clear();
        }
    }
}
