#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Effective;

#pragma warning disable CA1068 // CancellationToken parameters must come last

namespace Elffy
{
    internal sealed class OrderedParallelAsyncEventPromise<T> : IUniTaskSource, IChainInstancePooled<OrderedParallelAsyncEventPromise<T>>
    {
        private static Int16TokenFactory _tokenFactory;

        private OrderedParallelAsyncEventPromise<T>? _nextPooled;

        public ref OrderedParallelAsyncEventPromise<T>? NextPooled => ref _nextPooled;

        private OrderedParallelAsyncEventPromise(ArraySegment<Func<T, CancellationToken, UniTask>> funcs, T arg, CancellationToken ct, short version)
        {
            Ctor(funcs, arg, ct, version);
        }

        private void Ctor(ArraySegment<Func<T, CancellationToken, UniTask>> funcs, T arg, CancellationToken ct, short version)
        {
            throw new NotImplementedException();
        }

        public static UniTask CreateTask(ArraySegment<Func<T, CancellationToken, UniTask>> funcs, T arg, CancellationToken ct)
        {
            // [NOTE]
            // I don't do defensive copy. 'funcs' must be copied before the method is called.

            var token = _tokenFactory.CreateToken();
            if(ChainInstancePool<OrderedParallelAsyncEventPromise<T>>.TryGetInstanceFast(out var promise)) {
                promise.Ctor(funcs, arg, ct, token);
            }
            else {
                promise = new OrderedParallelAsyncEventPromise<T>(funcs, arg, ct, token);
            }
            return new UniTask(promise, token);
        }

        public void GetResult(short token)
        {
            throw new NotImplementedException();
        }

        public UniTaskStatus GetStatus(short token)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            throw new NotImplementedException();
        }

        public UniTaskStatus UnsafeGetStatus()
        {
            throw new NotImplementedException();
        }
    }
}
