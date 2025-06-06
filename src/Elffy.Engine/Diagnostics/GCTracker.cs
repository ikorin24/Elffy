﻿#nullable enable
using System;
using Elffy.Effective;

namespace Elffy.Diagnostics
{
    internal static class GCTracker
    {
        private static bool _isRunning;
        private static readonly int[] _gcCount = new int[GC.MaxGeneration + 1];

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
            DevEnv.ForceWriteLine($"----- GC gen {gen} -----");
            return true;
        }

        public static void End()
        {
            _isRunning = false;
            _gcCount.AsSpan().Clear();
        }
    }
}
