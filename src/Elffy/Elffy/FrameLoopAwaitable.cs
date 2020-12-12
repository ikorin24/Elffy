#nullable enable
using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace Elffy
{
    public readonly struct FrameLoopAwaitable
    {
        private readonly Awaiter _awaiter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal FrameLoopAwaitable(AsyncBackEndPoint endPoint, FrameLoopTiming eventType, CancellationToken cancellationToken)
        {
            _awaiter = new Awaiter(endPoint, eventType, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter GetAwaiter()
        {
            return _awaiter;
        }

        public async UniTask ContinueWith(Action continuation)
        {
            await this;
            continuation();
        }

        public async UniTask ContinueWith<T>(T state, Action<T> continuation)
        {
            await this;
            continuation(state);
        }

        public async UniTask<T> ContinueWith<T>(Func<T> continuation)
        {
            await this;
            return continuation();
        }

        public async UniTask<TResult> ContinueWith<T, TResult>(T state, Func<T, TResult> continuation)
        {
            await this;
            return continuation(state);
        }

        public async UniTask AsUniTask()
        {
            await this;
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            private readonly AsyncBackEndPoint _endPoint;
            private readonly FrameLoopTiming _eventType;
            private readonly CancellationToken _cancellationToken;

            public bool IsCompleted => false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Awaiter(AsyncBackEndPoint endPoint, FrameLoopTiming eventType, CancellationToken cancellationToken)
            {
                _endPoint = endPoint;
                _eventType = eventType;
                _cancellationToken = cancellationToken;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult()
            {
                _cancellationToken.ThrowIfCancellationRequested();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                _endPoint?.Post(_eventType, continuation);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                _endPoint?.Post(_eventType, continuation);
            }
        }
    }
}
