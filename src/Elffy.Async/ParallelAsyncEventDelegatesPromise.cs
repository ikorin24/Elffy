#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Elffy
{
    /*

    internal sealed class ParallelAsyncEventDelegatesPromise : IUniTaskSource
    {
        private const int NotEndFlag = 0;
        private const int EndFlag = 1;

        private int _isEnd;
        private int _completedCount;
        private int _taskCount;
        private Action<object>? _continuation;
        private object? _continuationState;

        private object? _error;     // null || ExceptionHolder || OperationCanceledException
        private short _version;

        //private UniTaskCompletionSourceCore<AsyncUnit> _core; // don't reset(called after GetResult, will invoke TrySetException.)

        private ParallelAsyncEventDelegatesPromise(ReadOnlySpan<Func<CancellationToken, UniTask>> funcs, CancellationToken ct)
        {
            _taskCount = funcs.Length;

            for(int i = 0; i < funcs.Length; i++) {
                UniTask.Awaiter awaiter;

                // I don't catch any exceptions here which is thrown synchronously.
                var task = funcs[i].Invoke(ct);

                awaiter = task.GetAwaiter();
                if(awaiter.IsCompleted) {
                    InvokeContinuationIfEnd(this, awaiter);
                }
                else {
                    // TODO: instance pooling of state
                    awaiter.SourceOnCompleted(static state =>
                    {
                        var s = (Tuple<ParallelAsyncEventDelegatesPromise, UniTask.Awaiter, int>)state;
                        InvokeContinuationIfEnd(s.Item1, s.Item2);
                    }, Tuple.Create(this, awaiter, i));
                }
            }
        }

        internal static UniTask CreateTask(ReadOnlySpan<Func<CancellationToken, UniTask>> funcs, CancellationToken ct)
        {
            // TODO: instance pooling
            var promise = new ParallelAsyncEventDelegatesPromise(funcs, ct);
            return new UniTask(promise, 0);
        }

        private static void InvokeContinuationIfEnd(ParallelAsyncEventDelegatesPromise self, in UniTask.Awaiter awaiter)
        {
            var needToEndImmediately = false;
            try {
                awaiter.GetResult();
            }
            catch(Exception ex) {
                needToEndImmediately = Interlocked.CompareExchange(ref self._isEnd, EndFlag, NotEndFlag) == NotEndFlag;
                if(needToEndImmediately) {
                    self._error = ex is OperationCanceledException ? ex : ExceptionHolder.Capture(ex);
                }
                else {

                    // TODO:

                    return;
                }
            }

            if(needToEndImmediately) {
                // [NOTE]
                // Don't move the following code inside the catch-block to avoid calling the continuation from it.
                // We must escape from the catch-block as fast as possible.

                self._core.OnCompleted(self._continuation, self._continuationState, self._core.Version);
                return;
            }

            if(Interlocked.Increment(ref self._completedCount) == self._taskCount) {
                if(self._continuation is not null && Interlocked.CompareExchange(ref self._isEnd, EndFlag, NotEndFlag) == NotEndFlag) {
                    self._core.TrySetResult(AsyncUnit.Default);
                    self._core.OnCompleted(self._continuation, self._continuationState, self._core.Version);
                }
            }
        }

        [DebuggerHidden]
        private bool TrySetException(Exception error)
        {
            _error = error is OperationCanceledException ? error : ExceptionHolder.Capture(error);
            if(error is OperationCanceledException) {
                _error = error;
            }
            else {
                _error = ExceptionHolder.Capture(error);
            }

            if(Interlocked.Increment(ref completedCount) == 1) {
                // setup result
                this.hasUnhandledError = true;
                if(error is OperationCanceledException) {
                    this.error = error;
                }
                else {
                    this.error = new ExceptionHolder(ExceptionDispatchInfo.Capture(error));
                }

                if(continuation != null || Interlocked.CompareExchange(ref this.continuation, UniTaskCompletionSourceCoreShared.s_sentinel, null) != null) {
                    continuation(continuationState);
                    return true;
                }
            }

            return false;
        }

        public void GetResult(short token) => _core.GetResult(token);

        public UniTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public UniTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if(continuation is null) { ThorwNullArg(nameof(continuation)); }
            ValidateToken(token);

            Volatile.Write(ref _continuationState, state);
            Volatile.Write(ref _continuation, continuation);

            if(_completedCount == _taskCount) {
                if(Interlocked.CompareExchange(ref _isEnd, EndFlag, NotEndFlag) == NotEndFlag) {
                    _core.OnCompleted(continuation, state, token);
                }
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateToken(short token)
        {
            if(token != _core.Version) {
                throw new InvalidOperationException("Token version is not matched, can not await twice or get Status after await.");
            }
        }

        [DebuggerHidden]
        [DoesNotReturn]
        private static void ThorwNullArg(string message) => throw new ArgumentNullException(message);
    }

    */

    internal sealed class ExceptionHolder
    {
        private ExceptionDispatchInfo? _info;

        private ExceptionHolder(ExceptionDispatchInfo info)
        {
            Debug.Assert(info is not null);
            _info = info;
        }

        [DebuggerHidden]
        public static ExceptionHolder Capture(Exception ex)
        {
            return new ExceptionHolder(ExceptionDispatchInfo.Capture(ex));
        }

        [DebuggerHidden]
        [DoesNotReturn]
        public void Throw()
        {
            var info = Interlocked.Exchange(ref _info, null);
            if(info != null) {
                GC.SuppressFinalize(this);
                info.Throw();
            }
            else {
                throw new InvalidOperationException($"Critical: Can not call {nameof(ExceptionHolder)}.{nameof(Throw)} twice !!!");
            }
        }

        ~ExceptionHolder()
        {
            // TODO: Report the unhandled exception.
        }
    }
}
