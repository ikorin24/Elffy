#nullable enable
using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace Elffy
{
    /// <summary>Frame loop awaitable object</summary>
    public readonly struct FrameLoopAwaitable
    {
        private readonly Awaiter _awaiter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal FrameLoopAwaitable(AsyncBackEndPoint endPoint, FrameLoopTiming eventType, CancellationToken cancellationToken)
        {
            _awaiter = new Awaiter(endPoint, eventType, cancellationToken);
        }

        /// <summary>Get awaiter of <see cref="FrameLoopAwaitable"/></summary>
        /// <returns>awaiter</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter GetAwaiter()
        {
            return _awaiter;
        }

        /// <summary>Creates a continuation that executes when <see cref="FrameLoopAwaitable"/> completes.</summary>
        /// <param name="continuation">a continuation action</param>
        /// <returns>awaitable object</returns>
        public async UniTask ContinueWith(Action continuation)
        {
            await this;
            continuation();
        }

        /// <summary>Creates a continuation that executes when <see cref="FrameLoopAwaitable"/> completes.</summary>
        /// <typeparam name="T">state type</typeparam>
        /// <param name="state">the argument of the continuation</param>
        /// <param name="continuation">a continuation action</param>
        /// <returns>awaitable object</returns>
        public async UniTask ContinueWith<T>(T state, Action<T> continuation)
        {
            await this;
            continuation(state);
        }

        /// <summary>Creates a continuation that executes when <see cref="FrameLoopAwaitable"/> completes.</summary>
        /// <typeparam name="T">state type</typeparam>
        /// <param name="continuation">a continuation function</param>
        /// <returns>awaitable object</returns>
        public async UniTask<T> ContinueWith<T>(Func<T> continuation)
        {
            await this;
            return continuation();
        }

        /// <summary>Creates a continuation that executes when <see cref="FrameLoopAwaitable"/> completes.</summary>
        /// <typeparam name="T">state type</typeparam>
        /// <typeparam name="TResult">the argument of the continuation</typeparam>
        /// <param name="state">the argument of the continuation</param>
        /// <param name="continuation">a continuation function</param>
        /// <returns>awaitable object</returns>
        public async UniTask<TResult> ContinueWith<T, TResult>(T state, Func<T, TResult> continuation)
        {
            await this;
            return continuation(state);
        }

        /// <summary>Convert <see cref="FrameLoopAwaitable"/> as <see cref="UniTask"/></summary>
        /// <returns>awaitable object</returns>
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
