#nullable enable
using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;

namespace Elffy.Features.Internal
{
    internal struct TimingAwaitableCore<TTimingPoint> where TTimingPoint : class, ITimingPoint
    {
        private static readonly object _completedTimingPoint = new object();

        private object? _timingPoint;       // ITimingPoint instance (pending) || '_completedTimingPoint' (completed) || null (after completed)
        private CancellationToken _cancellationToken;
        private short _token;

        public TimingAwaitableCore(TTimingPoint? timingPoint, short token, CancellationToken cancellationToken)
        {
            _timingPoint = timingPoint ?? _completedTimingPoint;
            _cancellationToken = cancellationToken;
            _token = token;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncUnit GetResult(short token)
        {
            ValidateToken(token);
            if(Interlocked.CompareExchange(ref _timingPoint, null, _completedTimingPoint) == _completedTimingPoint) {
                return AsyncUnit.Default;   // It means success
            }
            else {
                return NotSuccess();
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private AsyncUnit NotSuccess()
        {
            var status = UnsafeGetStatus();
            Debug.Assert(status != UniTaskStatus.Succeeded);    // 'Succeeded' never come here.
            return status switch
            {
                UniTaskStatus.Pending => throw new InvalidOperationException("Not yet completed, UniTask only allow to use await."),
                UniTaskStatus.Canceled => throw new OperationCanceledException(),
                UniTaskStatus.Succeeded or
                UniTaskStatus.Faulted or
                _ => throw new Exception("Invalid status. How did you get here ?"),
            };
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTaskStatus GetStatus(short token)
        {
            ValidateToken(token);
            return UnsafeGetStatus();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object?> continuation, object? state, short token)
        {
            ValidateToken(token);
            var timingPoint = Interlocked.Exchange(ref _timingPoint, _completedTimingPoint);
            if(timingPoint == _completedTimingPoint || timingPoint is null) {
                _timingPoint = null;
                ThrowForAwaitTwice();
            }
            else {
                Debug.Assert(timingPoint is TTimingPoint);
                Unsafe.As<TTimingPoint>(timingPoint).Post(continuation, state);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTaskStatus UnsafeGetStatus()
        {
            if(_timingPoint == _completedTimingPoint) {
                return UniTaskStatus.Succeeded;
            }
            else if(_cancellationToken.IsCancellationRequested) {
                return UniTaskStatus.Canceled;
            }
            else {
                return UniTaskStatus.Pending;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateToken(short token)
        {
            if(token != _token) { ThrowInvalidToken(); }
        }

        [DebuggerHidden]
        [DoesNotReturn]
        private static void ThrowInvalidToken() => throw new InvalidOperationException("Token version is not matched, can not await twice or get Status after await.");

        [DebuggerHidden]
        [DoesNotReturn]
        private static void ThrowForAwaitTwice() => throw new InvalidOperationException("Can not await twice");
    }
}
