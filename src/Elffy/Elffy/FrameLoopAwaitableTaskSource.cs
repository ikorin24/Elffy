#nullable enable
using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    internal sealed class FrameLoopAwaitableTaskSource : IUniTaskSource<AsyncUnit>
    {
        private static readonly object _completedEndPoint = new object();

        private object _endPoint;
        private FrameLoopTiming _timing;
        private CancellationToken _cancellationToken;

        internal static FrameLoopAwaitableTaskSource Create(AsyncBackEndPoint endPoint, FrameLoopTiming timing, CancellationToken cancellationToken)
        {
            // TODO: instance pooling
            var instance = new FrameLoopAwaitableTaskSource(endPoint, timing, cancellationToken);
            return instance;
        }

        private FrameLoopAwaitableTaskSource(AsyncBackEndPoint endPoint, FrameLoopTiming timing, CancellationToken cancellationToken)
        {
            InitFields(endPoint, timing, cancellationToken);
        }

        [MemberNotNull(nameof(_endPoint))]
        private void InitFields(AsyncBackEndPoint endPoint, FrameLoopTiming timing, CancellationToken cancellationToken)
        {
            // All fields must be set.
            if(timing == 0) {
                _endPoint = _completedEndPoint;
            }
            else {
                _endPoint = endPoint;
            }
            _timing = timing;
            _cancellationToken = cancellationToken;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncUnit GetResult(short token)
        {
            ValidateToken(token);
            return UnsafeGetStatus() switch
            {
                UniTaskStatus.Pending => ThrowInvalidOperation(),
                UniTaskStatus.Succeeded => AsyncUnit.Default,
                UniTaskStatus.Faulted => ThrowInvalidStatus(),      // The status will never be UniTaskStatus.Faulted
                UniTaskStatus.Canceled => ThrowCanceled(),
                _ => ThrowInvalidStatus(),
            };

            [DoesNotReturn]
            static AsyncUnit ThrowInvalidOperation() => throw new InvalidOperationException("Not yet completed, UniTask only allow to use await.");

            [DoesNotReturn]
            static AsyncUnit ThrowCanceled() => throw new OperationCanceledException();

            [DoesNotReturn]
            static AsyncUnit ThrowInvalidStatus() => throw new Exception("Invalid status. How did you get here ?");
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
            if(ReferenceEquals(endPoint, _completedEndPoint)) {
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
            return;
            //if(token != _token) {
            //    throw new InvalidOperationException("Token version is not matched, can not await twice or get Status after await.");
            //}
        }

        [DoesNotReturn]
        private static void ThrowForAwaitTwice() => throw new InvalidOperationException("Can not await twice");
    }
}
