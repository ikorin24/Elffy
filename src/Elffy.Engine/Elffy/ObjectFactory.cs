#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using System.ComponentModel;

namespace Elffy
{
    public static class ObjectFactory
    {
        private static readonly Dictionary<uint, Delegate> _dic = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register<T>(uint id, Func<UniTask<T>> generator)
        {
            if(_dic.ContainsKey(id)) {
                throw new ArgumentException($"id: {id} is already registered.");
            }
            _dic[id] = generator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register(uint id, Func<UniTask> generator)
        {
            if(_dic.ContainsKey(id)) {
                throw new ArgumentException($"id: {id} is already registered.");
            }
            _dic[id] = generator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<T> GenerateAsync<T>(uint id)
        {
            // throws exception if invalid type
            var g = (Func<UniTask<T>>)_dic[id];
            return UniTask.Create(g);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]       // Only for source generator (but method must be public)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<T> GenerateUnsafeAsync<T>(uint id)
        {
            // It does not check type. (If the type would be incompatible, VERY DENGEROUSE!!)
            var g = Unsafe.As<Func<UniTask<T>>>(_dic[id]);
            return UniTask.Create(g);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask GenerateNoneAsync(uint id)
        {
            // throws exception if invalid type
            var g = (Func<UniTask>)_dic[id];
            return UniTask.Create(g);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]       // Only for source generator (but method must be public)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask GenerateNoneUnsafeAsync<T>(uint id)
        {
            // It does not check type. (If the type would be incompatible, VERY DENGEROUSE!!)
            var g = Unsafe.As<Func<UniTask>>(_dic[id]);
            return UniTask.Create(g);
        }
    }
}
