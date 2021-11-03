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
    internal struct OrderedAsyncEventPromiseCore<T>
    {
        private PooledAsyncEventFuncs<Func<T, CancellationToken, UniTask>> _funcs;
        private Action<object>? _continuation;
        private object? _continuationState;
        private T _arg;     // It may be null if T is class. Be careful !
        private CancellationToken _cancellationToken;
        private CapturedExceptionWrapper _exception;
        private int _completedCount;
        private short _version;

        public PooledAsyncEventFuncs<Func<T, CancellationToken, UniTask>> Funcs => _funcs;
        public CancellationToken CancellationToken => _cancellationToken;
        public int CompletedCount => _completedCount;
        public CapturedExceptionWrapper Exception => _exception;
        public T Arg => _arg;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrderedAsyncEventPromiseCore(in PooledAsyncEventFuncs<Func<T, CancellationToken, UniTask>> funcs, T arg, CancellationToken ct, short version)
        {
            _funcs = funcs;
            _arg = arg;
            _cancellationToken = ct;
            _version = version;
            _continuation = null;
            _continuationState = null;
            _exception = CapturedExceptionWrapper.None;
            _completedCount = 0;
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        public void GetResultAndReset(short token)
        {
            ValidateToken(token);
            _version = 0;
            if(_completedCount < _funcs.Count) {
                UniTaskSourceHelper.ThrowNotCompleted();
            }
            _exception.ThrowIfCaptured();

            // Reset instance
            _version = 0;
            _funcs.Return();
            _funcs = default;
            _continuation = null;
            _continuationState = null;
            if(RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                _arg = default!;
            }
            _cancellationToken = default;
            _exception = CapturedExceptionWrapper.None;
            _completedCount = 0;
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTaskStatus GetStatus(short token)
        {
            ValidateToken(token);
            return UnsafeGetStatus();
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompletedCore(Action<object> continuation, object state, short token)
        {
            if(continuation is null) {
                UniTaskSourceHelper.ThrowNullArg(nameof(continuation));
            }
            ValidateToken(token);
            _continuationState = state;
            if(Interlocked.CompareExchange(ref _continuation, continuation, null) != null) {
                UniTaskSourceHelper.ThrowCannotAwaitTwice();
            }
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
        public void CaptureException(Exception ex)
        {
            _exception = CapturedExceptionWrapper.Capture(ex);
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeContinuation()
        {
            var continuation = Interlocked.Exchange(ref _continuation, UniTaskSourceHelper.ContinuationSentinel);
            Debug.Assert(continuation is not null);
            var state = _continuationState!;
            continuation(state);
        }


#if !DEBUG
        [DebuggerHidden]
#endif
        public void InvokeInnerTaskCompleted(in UniTask.Awaiter awaiter)
        {
            try {
                awaiter.GetResult();
            }
            catch(Exception ex) {
                _exception = CapturedExceptionWrapper.Capture(ex);
                _completedCount = _funcs.Count;
                return;
            }
            _completedCount += 1;
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
    }

    internal sealed class PromiseAndAwaiterPair : IChainInstancePooled<PromiseAndAwaiterPair>
    {
        private IUniTaskSource? _promise;
        private UniTask.Awaiter _awaiter;
        private PromiseAndAwaiterPair? _next;

        public ref PromiseAndAwaiterPair? NextPooled => ref _next;

        private PromiseAndAwaiterPair(IUniTaskSource promise, in UniTask.Awaiter awaiter)
        {
            _promise = promise;
            _awaiter = awaiter;
        }

        public static PromiseAndAwaiterPair Create<TPromise>(TPromise promise, in UniTask.Awaiter awaiter) where TPromise : class, IUniTaskSource
        {
            if(ChainInstancePool<PromiseAndAwaiterPair>.TryGetInstanceFast(out var instance)) {
                instance._promise = promise;
                instance._awaiter = awaiter;
            }
            else {
                instance = new PromiseAndAwaiterPair(promise, awaiter);
            }
            return instance;
        }

        public static (TPromise Promise, UniTask.Awaiter Awaiter) Extract<TPromise>(object obj) where TPromise : class, IUniTaskSource
        {
            Debug.Assert(obj is PromiseAndAwaiterPair);
            var s = Unsafe.As<PromiseAndAwaiterPair>(obj);
            Debug.Assert(s._promise is not null);
            Debug.Assert(s._promise is TPromise);
            var promise = Unsafe.As<TPromise>(s._promise);
            var awaiter = s._awaiter;
            s._awaiter = default;
            s._promise = default;
            ChainInstancePool<PromiseAndAwaiterPair>.ReturnInstanceFast(s);
            return (promise, awaiter);
        }
    }
}
