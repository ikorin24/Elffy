#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Effective
{
    public static class ChainInstancePool<T> where T : class, IChainInstancePooled<T>
    {
        private static int _maxPoolingCount = 1024;
        private static FastSpinLock _lock;
        private static T? _root;
        private static int _pooledCount;

        public static void SetMaxPoolingCount(int maxPoolingCount)
        {
            maxPoolingCount = Math.Max(0, maxPoolingCount);
            _lock.Enter();
            _maxPoolingCount = maxPoolingCount;
            _lock.Exit();
        }

        public static bool TryGetInstanceFast([MaybeNullWhen(false)] out T instance)
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
                    _pooledCount--;
                    _root = instance.NextPooled;
                    instance.NextPooled = null;
                    return true;
                }
                return false;
            }
            finally {
                _lock.Exit();
            }
        }

        public static bool TryGetInstance([MaybeNullWhen(false)] out T instance)
        {
            // Wait until the exclusion control is successfully obtained, I try to get the instance from the pool.
            try {
                _lock.Enter();
                instance = _root;
                if(instance is not null) {
                    Debug.Assert(_pooledCount > 0);
                    _pooledCount--;
                    _root = instance.NextPooled;
                    instance.NextPooled = null;
                    return true;
                }
                return false;
            }
            finally {
                _lock.Exit();
            }
        }

        public static void ReturnInstanceFast(T instance)
        {
            // If the exclusion control is successfully obtained, add the instance to the pool.
            if(_lock.TryEnter() == false) {
                return;
            }
            try {
                var pooledCount = _pooledCount;
                if(pooledCount >= _maxPoolingCount) { return; }
                var root = _root;
                _pooledCount = pooledCount + 1;
                _root = instance;
                if(root is not null) {
                    instance.NextPooled = root;
                }
            }
            finally {
                _lock.Exit();
            }
        }

        public static void ReturnInstance(T instance)
        {
            // Wait until the exclusion control is successfully obtained, I add the instance to the pool.
            try {
                _lock.Enter();
                var pooledCount = _pooledCount;
                if(pooledCount >= _maxPoolingCount) { return; }
                var root = _root;
                _pooledCount = pooledCount + 1;
                _root = instance;
                if(root is not null) {
                    instance.NextPooled = root;
                }
            }
            finally {
                _lock.Exit();
            }
        }
    }

    public interface IChainInstancePooled<T> where T : class
    {
        ref T? NextPooled { get; }
    }
}
