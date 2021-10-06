#nullable enable
using Elffy.Effective;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Features.Internal
{
    internal static class ChainInstancePool<T> where T : class, IChainInstancePooled<T>
    {
        private const int MaxPoolingCount = 1024;
        private static short _tokenFactory;
        private static FastSpinLock _lock;
        private static T? _root;
        private static int _pooledCount;

        public static bool TryGetInstance([MaybeNullWhen(false)] out T instance, out short token)
        {
            try {
                _lock.Enter();
                token = _tokenFactory;
                _tokenFactory = unchecked((short)(token + 1));
                instance = _root;
                if(instance is not null) {
                    Debug.Assert(_pooledCount > 0);
                    _pooledCount--;
                    _root = instance.NextPooling;
                    instance.NextPooling = null;
                    return true;
                }
                return false;
            }
            finally {
                _lock.Exit();
            }
        }

        public static void ReturnInstance(T instance)
        {
            // Add the instance to the pool.
            try {
                _lock.Enter();
                var pooledCount = _pooledCount;
                if(pooledCount == MaxPoolingCount) { return; }
                var root = _root;
                _pooledCount = pooledCount + 1;
                _root = instance;
                if(root is not null) {
                    instance.NextPooling = root;
                }
            }
            finally {
                _lock.Exit();
            }
        }
    }

    internal interface IChainInstancePooled<T> where T : class
    {
        ref T? NextPooling { get; }
    }
}
