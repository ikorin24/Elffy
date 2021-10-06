#nullable enable
using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Elffy.Features.Internal;

namespace Elffy
{
    internal sealed class FrameTimingAwaitableTaskSource : IUniTaskSource<AsyncUnit>, IChainInstancePooled<FrameTimingAwaitableTaskSource>
    {
        private static readonly object _completedTimingPoint = new object();

        private object? _timingPoint;       // FrameTimingPoint instance (pending) || '_completedTimingPoint' (completed) || null (after completed)
        private FrameTimingAwaitableTaskSource? _next;
        private CancellationToken _cancellationToken;
        private short _token;

        public ref FrameTimingAwaitableTaskSource? NextPooling => ref _next;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FrameTimingAwaitableTaskSource(FrameTimingPoint? timingPoint, short token, CancellationToken cancellationToken)
        {
            InitFields(timingPoint, token, cancellationToken);
            Debug.Assert(_next is null);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncUnit GetResult(short token)
        {
            ValidateToken(token);
            if(Interlocked.CompareExchange(ref _timingPoint, null, _completedTimingPoint) == _completedTimingPoint) {
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
            var timingPoint = Interlocked.Exchange(ref _timingPoint, _completedTimingPoint);
            if(ReferenceEquals(timingPoint, _completedTimingPoint) || timingPoint is null) {
                _timingPoint = null;
                ThrowForAwaitTwice();
            }
            else {
                Debug.Assert(timingPoint is FrameTimingPoint);
                Unsafe.As<FrameTimingPoint>(timingPoint).Post(continuation, state);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTaskStatus UnsafeGetStatus()
        {
            if(ReferenceEquals(_timingPoint, _completedTimingPoint)) {
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

        internal static UniTask<AsyncUnit> CreateTask(FrameTimingPoint? timingPoint, CancellationToken cancellationToken)
        {
            if(ChainInstancePool<FrameTimingAwaitableTaskSource>.TryGetInstance(out var instance, out var token)) {
                instance.InitFields(timingPoint, token, cancellationToken);
                return new UniTask<AsyncUnit>(instance, token);
            }
            else {
                return new UniTask<AsyncUnit>(new FrameTimingAwaitableTaskSource(timingPoint, token, cancellationToken), token);
            }
        }

        private static void Return(FrameTimingAwaitableTaskSource source)
        {
            // Clear the fields which is reference or contain reference type.
            Debug.Assert(source._next is null);
            Debug.Assert(source._timingPoint is null);
            source._cancellationToken = default;
            ChainInstancePool<FrameTimingAwaitableTaskSource>.ReturnInstance(source);
        }

        [MemberNotNull(nameof(_timingPoint))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitFields(FrameTimingPoint? timingPoint, short token, CancellationToken cancellationToken)
        {
            // All fields must be set except '_next'
            _timingPoint = timingPoint ?? _completedTimingPoint;
            _token = token;
            _cancellationToken = cancellationToken;
        }
    }
}
