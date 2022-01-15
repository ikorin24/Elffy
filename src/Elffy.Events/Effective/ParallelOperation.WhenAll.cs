#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace Elffy.Effective
{
    static partial class ParallelOperation
    {
        public static UniTask WhenAll<T>(ReadOnlySpan<UniTask<T>> tasks)
        {
            return new UniTask(WhenAllPromise.Create(tasks), 0);
        }

        public static UniTask WhenAll(ReadOnlySpan<UniTask> tasks)
        {
            return new UniTask(WhenAllPromise.Create(tasks), 0);
        }

        private sealed class PromiseAwaiterPair<TAwaiter> : IChainInstancePooled<PromiseAwaiterPair<TAwaiter>> where TAwaiter : struct
        {
            private IUniTaskSource? _promise;
            private TAwaiter _awaiter;
            private PromiseAwaiterPair<TAwaiter>? _next;

            public ref PromiseAwaiterPair<TAwaiter>? NextPooled => ref _next;

            private PromiseAwaiterPair(IUniTaskSource promise, in TAwaiter awaiter)
            {
                _promise = promise;
                _awaiter = awaiter;
            }

            public static PromiseAwaiterPair<TAwaiter> Create<TPromise>(TPromise promise, in TAwaiter awaiter) where TPromise : class, IUniTaskSource
            {
                if(ChainInstancePool<PromiseAwaiterPair<TAwaiter>>.TryGetInstanceFast(out var instance)) {
                    instance._promise = promise;
                    instance._awaiter = awaiter;
                }
                else {
                    instance = new PromiseAwaiterPair<TAwaiter>(promise, awaiter);
                }
                return instance;
            }

            public static (TPromise Promise, TAwaiter Awaiter) Extract<TPromise>(object obj) where TPromise : class, IUniTaskSource
            {
                Debug.Assert(obj is PromiseAwaiterPair<TAwaiter>);
                var s = Unsafe.As<PromiseAwaiterPair<TAwaiter>>(obj);
                Debug.Assert(s._promise is not null);
                Debug.Assert(s._promise is TPromise);
                var promise = Unsafe.As<TPromise>(s._promise);
                var awaiter = s._awaiter;
                s._awaiter = default;
                s._promise = default;
                ChainInstancePool<PromiseAwaiterPair<TAwaiter>>.ReturnInstanceFast(s);
                return (promise, awaiter);
            }
        }

        private sealed class WhenAllPromise : IUniTaskSource, IValueTaskSource
        {
            private int _completeCount;
            private int _tasksLength;
            private UniTaskCompletionSourceCore<AsyncUnit> _core;

            private WhenAllPromise()
            {
            }

            private void Init(ReadOnlySpan<UniTask> tasks)
            {
                _tasksLength = tasks.Length;
                _completeCount = 0;
                if(tasks.Length == 0) {
                    _core.TrySetResult(AsyncUnit.Default);
                    return;
                }

                for(int i = 0; i < tasks.Length; i++) {
                    UniTask.Awaiter awaiter;
                    try {
                        awaiter = tasks[i].GetAwaiter();
                    }
                    catch(Exception error) {
                        _core.TrySetException(error);
                        continue;
                    }

                    if(awaiter.IsCompleted) {
                        TryInvokeContinuation(this, in awaiter);
                        continue;
                    }

                    awaiter.SourceOnCompleted(static state =>
                    {
                        var (self, awaiter) = PromiseAwaiterPair<UniTask.Awaiter>.Extract<WhenAllPromise>(state);
                        TryInvokeContinuation(self, in awaiter);
                    }, PromiseAwaiterPair<UniTask.Awaiter>.Create(this, awaiter));
                }
            }

            private void Init<T>(ReadOnlySpan<UniTask<T>> tasks)
            {
                _tasksLength = tasks.Length;
                _completeCount = 0;
                if(tasks.Length == 0) {
                    _core.TrySetResult(AsyncUnit.Default);
                    return;
                }

                for(int i = 0; i < tasks.Length; i++) {
                    UniTask<T>.Awaiter awaiter;
                    try {
                        awaiter = tasks[i].GetAwaiter();
                    }
                    catch(Exception error) {
                        _core.TrySetException(error);
                        continue;
                    }

                    if(awaiter.IsCompleted) {
                        TryInvokeContinuation(this, in awaiter);
                        continue;
                    }

                    awaiter.SourceOnCompleted(static state =>
                    {
                        var (self, awaiter) = PromiseAwaiterPair<UniTask<T>.Awaiter>.Extract<WhenAllPromise>(state);
                        TryInvokeContinuation(self, in awaiter);
                    }, PromiseAwaiterPair<UniTask<T>.Awaiter>.Create(this, awaiter));
                }
            }

            public static WhenAllPromise Create(ReadOnlySpan<UniTask> tasks)
            {
                var promise = new WhenAllPromise();
                promise.Init(tasks);
                return promise;
            }

            public static WhenAllPromise Create<T>(ReadOnlySpan<UniTask<T>> tasks)
            {
                var promise = new WhenAllPromise();
                promise.Init(tasks);
                return promise;
            }

            private static void TryInvokeContinuation(WhenAllPromise self, in UniTask.Awaiter awaiter)
            {
                try {
                    awaiter.GetResult();
                }
                catch(Exception ex) {
                    self._core.TrySetException(ex);
                    return;
                }

                if(Interlocked.Increment(ref self._completeCount) == self._tasksLength) {
                    self._core.TrySetResult(AsyncUnit.Default);
                }
            }

            private static void TryInvokeContinuation<T>(WhenAllPromise self, in UniTask<T>.Awaiter awaiter)
            {
                try {
                    awaiter.GetResult();
                }
                catch(Exception ex) {
                    self._core.TrySetException(ex);
                    return;
                }

                if(Interlocked.Increment(ref self._completeCount) == self._tasksLength) {
                    self._core.TrySetResult(AsyncUnit.Default);
                }
            }

            public void GetResult(short token)
            {
#pragma warning disable CA1816
                GC.SuppressFinalize(this);
#pragma warning restore CA1816
                _core.GetResult(token);
            }

            public UniTaskStatus GetStatus(short token) => _core.GetStatus(token);

            public UniTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

            public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);
        }
    }
}
