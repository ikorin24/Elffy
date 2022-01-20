#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    public static class EngineSetting
    {
        private static readonly object _syncObj = new object();
        private static bool _isLocked;

        private static bool _enableContextAssociatedMemorySafety = true;
        private static UserCodeExceptionCatchMode _userCodeExceptionCatchMode = UserCodeExceptionCatchMode.Default;

        public static bool EnableContextAssociatedMemorySafety
        {
            get => _enableContextAssociatedMemorySafety;
            set => SyncSet(ref _enableContextAssociatedMemorySafety, value);
        }

        internal static UserCodeExceptionCatchMode UserCodeExceptionCatchMode
        {
            get => _userCodeExceptionCatchMode;
            set => SyncSet(ref _userCodeExceptionCatchMode, value);
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
        private static void ThrowAlreadyLocked() => throw new InvalidOperationException($"{nameof(EngineSetting)} is locked. Set values before the Engine runs.");
    }

    internal enum UserCodeExceptionCatchMode
    {
        Default = 0,
        Throw = 1,
    }
}
