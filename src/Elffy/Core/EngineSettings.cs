#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Core
{
    public static class EngineSettings
    {
        private static readonly object _syncObj = new object();
        private static bool _isLocked;

        private static bool _enableContextBindedMemorySafety = true;

        public static bool EnableContextBindedMemorySafety
        {
            get => _enableContextBindedMemorySafety;
            set => SyncSet(ref _enableContextBindedMemorySafety, true);
        }


        internal static void Lock()
        {
            // This method is called when the Engine runs.

            lock(_syncObj) {
                _isLocked = true;
            }
        }

        internal static void Unlock()
        {
            // This method is called when the Engine stops

            lock(_syncObj) {
                _isLocked = false;
            }
        }

        private static void SyncSet<T>(ref T field, T value)
        {
            lock(_syncObj) {
                if(_isLocked) { ThrowAlreadyLocked(); }
                field = value;
            }
        }

        [DoesNotReturn]
        private static void ThrowAlreadyLocked() => throw new InvalidOperationException($"{nameof(EngineSettings)} is locked. Set values before the Engine runs.");
    }
}
