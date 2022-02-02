#nullable enable
using Elffy.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Elffy
{
    internal static class ArrayPoolForEventRaiser
    {
        internal const int LengthOfPoolTargetArray = 4; // Don't change

        private const int MaxPoolingCount = 512;
        private static FastSpinLock _lock;
        private static object?[]? _root;
        private static int _pooledCount;

        public static bool TryGetArray4Fast([MaybeNullWhen(false)] out object?[] instance)
        {
            // If the exclusion control is successfully obtained, I try to get the instance from the pool.
            if(_lock.TryEnter() == false) {
                instance = null;
                return false;
            }
            try {
                instance = _root;
                if(instance is not null) {
                    Debug.Assert(_pooledCount > 0);
                    Debug.Assert(instance.Length == LengthOfPoolTargetArray);
                    _pooledCount--;
                    _root = SafeCast.As<object?[]>(MemoryMarshal.GetArrayDataReference(instance));  // _root = (object?[])instance[0];
                    MemoryMarshal.GetArrayDataReference(instance) = null;                           // instance[0] = null;
                    return true;
                }
                return false;
            }
            finally {
                _lock.Exit();
            }
        }

        public static bool TryGetArray4([MaybeNullWhen(false)] out object?[] instance)
        {
            _lock.Enter();
            try {
                instance = _root;
                if(instance is not null) {
                    Debug.Assert(_pooledCount > 0);
                    Debug.Assert(instance.Length == LengthOfPoolTargetArray);
                    _pooledCount--;
                    _root = SafeCast.As<object?[]>(MemoryMarshal.GetArrayDataReference(instance));  // _root = (object?[])instance[0];
                    MemoryMarshal.GetArrayDataReference(instance) = null;                           // instance[0] = null;
                    return true;
                }
                return false;
            }
            finally {
                _lock.Exit();
            }
        }

        public static void ReturnArray4Fast(object?[] instance)
        {
            Debug.Assert(instance.Length == LengthOfPoolTargetArray);
#if DEBUG
            Debug.Assert(instance.GetType() == typeof(object[]));
            for(int i = 0; i < LengthOfPoolTargetArray; i++) {
                Debug.Assert(instance[i] == null);
            }
#endif

            // If the exclusion control is successfully obtained, add the instance to the pool.
            if(_lock.TryEnter() == false) {
                return;
            }
            try {
                var pooledCount = _pooledCount;
                if(pooledCount == MaxPoolingCount) { return; }
                var root = _root;
                _pooledCount = pooledCount + 1;
                _root = instance;
                if(root is not null) {
                    MemoryMarshal.GetArrayDataReference(instance) = root;   // instance[0] = root;
                }
            }
            finally {
                _lock.Exit();
            }
        }
    }
}
