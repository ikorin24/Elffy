#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Effective;

#pragma warning disable CA1068 // CancellationToken parameters must come last

namespace Elffy
{
    internal sealed class OrderedSequentialAsyncEventPromise<T> : IUniTaskSource, IChainInstancePooled<OrderedSequentialAsyncEventPromise<T>>
    {
        private static Int16TokenFactory _tokenFactory;

        private OrderedSequentialAsyncEventPromise<T>? _nextPooled;
        private OrderedAsyncEventPromiseCore<T> _core;

        public ref OrderedSequentialAsyncEventPromise<T>? NextPooled => ref _nextPooled;

        private OrderedSequentialAsyncEventPromise(ArraySegment<Func<T, CancellationToken, UniTask>> funcs, T arg, CancellationToken ct, short version)
        {
            _core = new OrderedAsyncEventPromiseCore<T>(funcs, arg, ct, version);
        }

        public static UniTask CreateTask(ArraySegment<Func<T, CancellationToken, UniTask>> funcs, T arg, CancellationToken ct)
        {
            // [NOTE]
            // I don't do defensive copy. 'funcs' must be copied before the method is called.

            var token = _tokenFactory.CreateToken();
            if(ChainInstancePool<OrderedSequentialAsyncEventPromise<T>>.TryGetInstanceFast(out var promise)) {
                promise._core = new OrderedAsyncEventPromiseCore<T>(funcs, arg, ct, token);
            }
            else {
                promise = new OrderedSequentialAsyncEventPromise<T>(funcs, arg, ct, token);
            }
            return new UniTask(promise, token);
        }

#if !DEBUG
        [System.Diagnostics.DebuggerHidden]
#endif
        public void GetResult(short token)
        {
            _core.GetResultAndReset(token);
            ChainInstancePool<OrderedSequentialAsyncEventPromise<T>>.ReturnInstanceFast(this);
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

        NEXT_TASK:
            var index = _core.CompletedCount;
            if(index == funcs.Count || _core.Exception.Status != UniTaskCapturedExceptionStatus.None) {
                _core.InvokeContinuation();
                return;
            }
            UniTask.Awaiter awaiter;
            try {
                var task = funcs[index].Invoke(_core.Arg, ct);
                awaiter = task.GetAwaiter();
            }
            catch(Exception ex) {
                _core.CaptureException(ex);
                goto NEXT_TASK;
            }

            if(awaiter.IsCompleted) {
                _core.InvokeInnerTaskCompleted(awaiter);
                goto NEXT_TASK;
            }
            awaiter.SourceOnCompleted(static s =>
            {
                var (self, awaiter) = PromiseAndAwaiterPair.Extract<OrderedSequentialAsyncEventPromise<T>>(s);
                self._core.InvokeInnerTaskCompleted(awaiter);
                self.ExecuteNextTask();
            }, PromiseAndAwaiterPair.Create(this, awaiter));
        }
    }
}
