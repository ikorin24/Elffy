#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        private OrderedAsyncEventPromiseCore<T> _core;
        private bool _isCompletedSuccessfully;

        public ref OrderedParallelAsyncEventPromise<T>? NextPooled => ref _nextPooled;

        private OrderedParallelAsyncEventPromise(in PooledAsyncEventFuncs<Func<T, CancellationToken, UniTask>> funcs, T arg, CancellationToken ct, short version)
        {
            _core = new OrderedAsyncEventPromiseCore<T>(funcs, arg, ct, version);
        }

        public static UniTask CreateTask(in PooledAsyncEventFuncs<Func<T, CancellationToken, UniTask>> funcs, T arg, CancellationToken ct)
        {
            // [NOTE]
            // I don't do defensive copy. 'funcs' must be copied before the method is called.

            var token = _tokenFactory.CreateToken();
            if(ChainInstancePool<OrderedParallelAsyncEventPromise<T>>.TryGetInstanceFast(out var promise)) {
                promise._core = new OrderedAsyncEventPromiseCore<T>(funcs, arg, ct, token);
            }
            else {
                promise = new OrderedParallelAsyncEventPromise<T>(funcs, arg, ct, token);
            }
            return new UniTask(promise, token);
        }

#if !DEBUG
        [System.Diagnostics.DebuggerHidden]
#endif
        public void GetResult(short token)
        {
            _core.GetResultAndReset(token);
            if(_isCompletedSuccessfully) {
                // [NOTE]
                // If an exception interrupts an internal task, there is no way to ensure that all other tasks are interrupted.
                // Therefore, I pool the instance only when all tasks have finished successfully.
                _isCompletedSuccessfully = false;
                ChainInstancePool<OrderedParallelAsyncEventPromise<T>>.ReturnInstanceFast(this);
            }
        }

#if !DEBUG
        [System.Diagnostics.DebuggerHidden]
#endif
        public UniTaskStatus GetStatus(short token) => _core.GetStatus(token);

#if !DEBUG
        [System.Diagnostics.DebuggerHidden]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

#if !DEBUG
        [System.Diagnostics.DebuggerHidden]
#endif
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _core.OnCompletedCore(continuation, state, token);
            ExecuteNextTask();
        }

#if !DEBUG
        [System.Diagnostics.DebuggerHidden]
#endif
        private void ExecuteNextTask()
        {
            var funcs = _core.Funcs;
            var ct = _core.CancellationToken;

            var index = 0;
        NEXT_TASK:
            if(index >= funcs.Count) {
                return;
            }
            UniTask.Awaiter awaiter;
            try {
                var f = funcs[index++];
                Debug.Assert(f is not null);
                var task = f.Invoke(_core.Arg, ct);
                awaiter = task.GetAwaiter();
            }
            catch(Exception ex) {
                _core.CaptureException(ex);
                _isCompletedSuccessfully = false;
                _core.InvokeContinuation();
                return;
            }

            if(awaiter.IsCompleted) {
                _core.InvokeInnerTaskCompleted(awaiter, out var callContinuationNeeded, out var successflyCompleted);
                if(callContinuationNeeded) {
                    _isCompletedSuccessfully = successflyCompleted;
                    _core.InvokeContinuation();
                }
                goto NEXT_TASK;
            }
            awaiter.SourceOnCompleted(static s =>
            {
                var (self, awaiter) = PromiseAndAwaiterPair.Extract<OrderedParallelAsyncEventPromise<T>>(s);
                ref var core = ref self._core;
                core.InvokeInnerTaskCompleted(awaiter, out var callContinuationNeeded, out var successflyCompleted);
                if(callContinuationNeeded) {
                    self._isCompletedSuccessfully = successflyCompleted;
                    self._core.InvokeContinuation();
                }
            }, PromiseAndAwaiterPair.Create(this, awaiter));
            goto NEXT_TASK;
        }
    }
}
