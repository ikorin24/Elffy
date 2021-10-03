#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Elffy
{
    public sealed class FrameTimingPoint
    {
        private readonly IHostScreen _screen;
        private readonly AsyncBackEndPoint _endPoints;
        private readonly ConcurrentQueue<WorkItem> _queue;
        private readonly FrameTiming _timing;

        private CurrentFrameTiming CurrentTiming => _screen.CurrentTiming;

        public IHostScreen Screen => _screen;
        public FrameTiming TargetTiming => _timing;

        internal FrameTimingPoint(AsyncBackEndPoint endPoints, FrameTiming timing)
        {
            Debug.Assert(timing.IsSpecified());
            _screen = endPoints.Screen;
            _endPoints = endPoints;
            _queue = new ConcurrentQueue<WorkItem>();
            _timing = timing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrameAsyncEnumerable Frames(CancellationToken cancellationToken = default)
        {
            return _screen.Frames(_timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<AsyncUnit> Switch(CancellationToken cancellationToken = default)
        {
            return FrameTimingAwaitableTaskSource.CreateTask(_endPoints, _timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<AsyncUnit> Ensure(CancellationToken cancellationToken = default)
        {
            if(CurrentTiming == _timing) {
                return new UniTask<AsyncUnit>(AsyncUnit.Default);
            }
            return Switch(cancellationToken);
        }

        /// <summary>Wait for the specified number of frames.</summary>
        /// <param name="frameCount">number for frames</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        public async UniTask<AsyncUnit> DelayFrame(int frameCount, CancellationToken cancellationToken = default)
        {
            if(frameCount < 0) {
                ThrowOutOfRange();
                static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(frameCount));
            }
            for(int i = 0; i < frameCount; i++) {
                await Switch(cancellationToken);
            }
            return AsyncUnit.Default;
        }

        /// <summary>Wait for the specified time.</summary>
        /// <param name="time">time span for waiting</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        public async UniTask<AsyncUnit> DelayTime(TimeSpan time, CancellationToken cancellationToken = default)
        {
            var start = _screen.Time;
            while(_screen.Time - start <= time) {
                await Switch(cancellationToken);
            }
            return AsyncUnit.Default;
        }

        /// <summary>Wait for the specified time.</summary>
        /// <param name="millisecond">millisecond time for waiting</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<AsyncUnit> DelayTime(int millisecond, CancellationToken cancellationToken = default)
        {
            return DelayTime(TimeSpan.FromMilliseconds(millisecond), cancellationToken);
        }

        /// <summary>Wait for the specified real time.</summary>
        /// <param name="time">time span for waiting</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        public async UniTask<AsyncUnit> DelayRealTime(TimeSpan time, CancellationToken cancellationToken = default)
        {
            var start = Engine.RunningRealTime;
            while(Engine.RunningRealTime - start <= time) {
                await Switch(cancellationToken);
            }
            return AsyncUnit.Default;
        }

        /// <summary>Wait for the specified real time.</summary>
        /// <param name="millisecond">millisecond time for waiting</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<AsyncUnit> DelayRealTime(int millisecond, CancellationToken cancellationToken = default)
        {
            return DelayRealTime(TimeSpan.FromMilliseconds(millisecond), cancellationToken);
        }

        internal void AbortAllEvents()
        {
            _queue.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DoQueuedEvents()
        {
            var queue = _queue;
            var count = queue.Count;
            if(count > 0) {
                Do(queue, count);

                [DebuggerHidden]
                static void Do(ConcurrentQueue<WorkItem> queue, int count)
                {
                    for(int i = 0; i < count; i++) {
                        var exists = queue.TryDequeue(out var action);
                        Debug.Assert(exists);
                        try {
                            action.Invoke();
                        }
                        catch {
                            // Don't throw
                        }
                    }
                }
            }
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Post(Action continuation)
        {
            if(continuation is null) { return; }
            _queue.Enqueue(new WorkItem(continuation));
        }

        public void Post(Action<object?> continuation, object? state)
        {
            if(continuation is null) { return; }
            _queue.Enqueue(new WorkItem(continuation, state));
        }

        private readonly struct WorkItem
        {
            private readonly Action<object?> _action;
            private readonly object? _state;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public WorkItem(Action<object?> action, object? state)
            {
                _action = action;
                _state = state;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public WorkItem(Action action)
            {
                //_action = state =>
                //{
                //    Debug.Assert(state is not null);
                //    SafeCast.As<Action>(state).Invoke();
                //};
                _action = Lambda.Instance.Action;
                _state = action;
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Invoke() => _action.Invoke(_state);

            private sealed class Lambda
            {
                public static readonly Lambda Instance = new Lambda();
                public readonly Action<object?> Action;
                private Lambda() => Action = new Action<object?>(M);

                [DebuggerHidden]
                private void M(object? state)
                {
                    Debug.Assert(state is not null);
                    SafeCast.As<Action>(state).Invoke();
                }
            }
        }
    }
}
