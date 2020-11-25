#nullable enable
using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace Elffy.Threading.Tasks
{
    public readonly struct FrameLoopAwaitable
    {
        private readonly AsyncBackEndPoint _asyncBack;
        private readonly FrameLoopTiming _eventType;
        private readonly CancellationToken _cancellationToken;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrameLoopAwaitable(AsyncBackEndPoint asyncBack, FrameLoopTiming eventType, CancellationToken cancellationToken)
        {
            _asyncBack = asyncBack!;
            _eventType = eventType;
            _cancellationToken = cancellationToken;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter GetAwaiter()
        {
            return new Awaiter(_asyncBack, _eventType, _cancellationToken);
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

        public readonly struct Awaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            private readonly AsyncBackEndPoint _asyncBack;
            private readonly FrameLoopTiming _eventType;
            private readonly CancellationToken _cancellationToken;

            public bool IsCompleted => false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Awaiter(AsyncBackEndPoint asyncBack, FrameLoopTiming eventType, CancellationToken cancellationToken)
            {
                _asyncBack = asyncBack;
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
                _asyncBack?.Post(_eventType, continuation);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                _asyncBack?.Post(_eventType, continuation);
            }
        }
    }
}
