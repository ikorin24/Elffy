﻿#nullable enable
using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    internal sealed partial class FrameLoopAwaitableTaskSource : IUniTaskSource<AsyncUnit>
    {
        private static readonly object _completedEndPoint = new object();

        private object? _endPoint;      // AsyncBackEndPoint instance (pending) || '_completedEndPoint' (completed) || null (after completed)
        private FrameLoopAwaitableTaskSource? _next;
        private CancellationToken _cancellationToken;
        private short _token;
        private FrameLoopTiming _timing;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncUnit GetResult(short token)
        {
            ValidateToken(token);
            if(Interlocked.CompareExchange(ref _endPoint, null, _completedEndPoint) == _completedEndPoint) {
                Return(this);
                return AsyncUnit.Default;   // It means success
            }
            else {
                return NotSuccess();

                [DebuggerHidden]
                [MethodImpl(MethodImplOptions.NoInlining)]
                AsyncUnit NotSuccess()
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
            }
        }

        [DebuggerHidden]
        void IUniTaskSource.GetResult(short token) => GetResult(token);

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
            var endPoint = Interlocked.Exchange(ref _endPoint, _completedEndPoint);
            if(ReferenceEquals(endPoint, _completedEndPoint) || endPoint is null) {
                _endPoint = null;
                ThrowForAwaitTwice();
            }
            else {
                Debug.Assert(endPoint is AsyncBackEndPoint);
                Unsafe.As<AsyncBackEndPoint>(endPoint).Post(_timing, continuation, state);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTaskStatus UnsafeGetStatus()
        {
            if(ReferenceEquals(_endPoint, _completedEndPoint)) {
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
            if(token != _token) {
                throw new InvalidOperationException("Token version is not matched, can not await twice or get Status after await.");
            }
        }

        [DebuggerHidden]
        [DoesNotReturn]
        private static void ThrowForAwaitTwice() => throw new InvalidOperationException("Can not await twice");
    }
}
