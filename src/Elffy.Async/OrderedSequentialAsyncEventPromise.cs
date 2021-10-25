#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Elffy.Effective;

#pragma warning disable CA1068 // CancellationToken parameters must come last

namespace Elffy
{
    internal sealed class OrderedSequentialAsyncEventPromise<T> : IUniTaskSource, IChainInstancePooled<OrderedSequentialAsyncEventPromise<T>>
    {
        private static Int16TokenFactory _tokenFactory;

        private OrderedSequentialAsyncEventPromise<T>? _nextPooled;
        private ArraySegment<Func<T, CancellationToken, UniTask>> _funcs;
        private Action<object>? _continuation;
        private object? _continuationState;
        private T _arg;     // It may be null if T is class. Be careful !
        private CancellationToken _cancellationToken;
        private CapturedExceptionWrapper _exception;
        private int _completedCount;
        private short _version;

        public ref OrderedSequentialAsyncEventPromise<T>? NextPooled => ref _nextPooled;

        // [NOTE]
        // The warning CS8618 says that '_arg' should be T as nullable reference type,
        // but 'T?' means 'Nullable<T>' when T is value type.
        // I don't know how to deal it.
#pragma warning disable CS8618
        private OrderedSequentialAsyncEventPromise(ArraySegment<Func<T, CancellationToken, UniTask>> funcs, T arg, CancellationToken ct, short version)
#pragma warning restore CS8618
        {
            Ctor(funcs, arg, ct, version);
        }

        private void Ctor(ArraySegment<Func<T, CancellationToken, UniTask>> funcs, T arg, CancellationToken ct, short version)
        {
            _funcs = funcs;
            _arg = arg;
            _cancellationToken = ct;
            _version = version;
        }

        public static UniTask CreateTask(ArraySegment<Func<T, CancellationToken, UniTask>> funcs, T arg, CancellationToken ct)
        {
            // [NOTE]
            // I don't do defensive copy. 'funcs' must be copied before the method is called.

            var token = _tokenFactory.CreateToken();
            if(ChainInstancePool<OrderedSequentialAsyncEventPromise<T>>.TryGetInstanceFast(out var promise)) {
                promise.Ctor(funcs, arg, ct, token);
            }
            else {
                promise = new OrderedSequentialAsyncEventPromise<T>(funcs, arg, ct, token);
            }
            return new UniTask(promise, token);
        }

        private void ResetAndPoolInstance()
        {
            _version = 0;
            _funcs = default;
            _continuation = null;
            _continuationState = null;
            if(RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                _arg = default!;
            }
            _cancellationToken = default;
            _exception = CapturedExceptionWrapper.None;
            _completedCount = 0;
            ChainInstancePool<OrderedSequentialAsyncEventPromise<T>>.ReturnInstanceFast(this);
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        public void GetResult(short token)
        {
            ValidateToken(token);
            _version = 0;
            if(_completedCount < _funcs.Count) {
                UniTaskSourceHelper.ThrowNotCompleted();
            }
            _exception.ThrowIfCaptured();
            ResetAndPoolInstance();
            return;
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        public UniTaskStatus GetStatus(short token)
        {
            ValidateToken(token);
            return UnsafeGetStatus();
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if(continuation is null) {
                UniTaskSourceHelper.ThrowNullArg(nameof(continuation));
            }
            ValidateToken(token);
            _continuationState = state;
            if(Interlocked.CompareExchange(ref _continuation, continuation, null) != null) {
                UniTaskSourceHelper.ThrowCannotAwaitTwice();
            }
            ExecuteNextTask();
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTaskStatus UnsafeGetStatus()
        {
            var exceptionStatus = _exception.Status;
            if(exceptionStatus == UniTaskCapturedExceptionStatus.None) {
                if(_completedCount < _funcs.Count) {
                    return UniTaskStatus.Pending;
                }
                else {
                    return UniTaskStatus.Succeeded;
                }
            }
            else {
                if(exceptionStatus == UniTaskCapturedExceptionStatus.Canceled) {
                    return UniTaskStatus.Canceled;
                }
                else {
                    Debug.Assert(exceptionStatus == UniTaskCapturedExceptionStatus.Faulted);
                    return UniTaskStatus.Faulted;
                }
            }
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        private void ExecuteNextTask()
        {
            var funcs = _funcs;
            var ct = _cancellationToken;

        NEXT_TASK:
            var index = _completedCount;
            if(index == _funcs.Count || _exception.Status != UniTaskCapturedExceptionStatus.None) {
                InvokeContinuation(this);
                return;
            }
            UniTask task;
            UniTask.Awaiter awaiter;
            try {
                task = funcs[index].Invoke(_arg, ct);
                awaiter = task.GetAwaiter();
            }
            catch(Exception ex) {
                _exception = CapturedExceptionWrapper.Capture(ex);
                goto NEXT_TASK;
            }

            if(awaiter.IsCompleted) {
                InvokeInnerTaskCompleted(this, awaiter);
                goto NEXT_TASK;
            }
            else {
                awaiter.SourceOnCompleted(static s =>
                {
                    var (self, awaiter) = InnerTaskState.Extract(s);
                    InvokeInnerTaskCompleted(self, awaiter);
                    self.ExecuteNextTask();
                }, InnerTaskState.Create(this, awaiter));
            }
            return;

#if !DEBUG
            [DebuggerHidden]
#endif
            static void InvokeInnerTaskCompleted(OrderedSequentialAsyncEventPromise<T> self, in UniTask.Awaiter awaiter)
            {
                try {
                    awaiter.GetResult();
                }
                catch(Exception ex) {
                    self._exception = CapturedExceptionWrapper.Capture(ex);
                    self._completedCount = self._funcs.Count;
                    return;
                }
                self._completedCount += 1;
            }

#if !DEBUG
            [DebuggerHidden]
#endif
            static void InvokeContinuation(OrderedSequentialAsyncEventPromise<T> self)
            {
                var continuation = Interlocked.Exchange(ref self._continuation, UniTaskSourceHelper.ContinuationSentinel);
                Debug.Assert(continuation is not null);
                var state = self._continuationState!;
                continuation(state);
            }
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateToken(short token)
        {
            if(token != _version) {
                throw new InvalidOperationException("Token version is not matched, can not await twice or get Status after await.");
            }
        }

        private sealed class InnerTaskState : IChainInstancePooled<InnerTaskState>
        {
            private OrderedSequentialAsyncEventPromise<T>? _promise;
            private UniTask.Awaiter _awaiter;
            private InnerTaskState? _next;

            public ref InnerTaskState? NextPooled => ref _next;

            private InnerTaskState(OrderedSequentialAsyncEventPromise<T> promise, in UniTask.Awaiter awaiter)
            {
                _promise = promise;
                _awaiter = awaiter;
            }

            public static InnerTaskState Create(OrderedSequentialAsyncEventPromise<T> promise, in UniTask.Awaiter awaiter)
            {
                if(ChainInstancePool<InnerTaskState>.TryGetInstanceFast(out var instance)) {
                    instance._promise = promise;
                    instance._awaiter = awaiter;
                }
                else {
                    instance = new InnerTaskState(promise, awaiter);
                }
                return instance;
            }

            public static (OrderedSequentialAsyncEventPromise<T> Promise, UniTask.Awaiter Awaiter) Extract(object obj)
            {
                Debug.Assert(obj is InnerTaskState);
                var s = Unsafe.As<InnerTaskState>(obj);
                Debug.Assert(s._promise is not null);
                var promise = s._promise;
                var awaiter = s._awaiter;
                s._awaiter = default;
                s._promise = default;
                ChainInstancePool<InnerTaskState>.ReturnInstanceFast(s);
                return (promise, awaiter);
            }
        }
    }
}
