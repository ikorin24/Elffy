#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Features.Internal;

namespace Elffy
{
    [DebuggerDisplay("{DebuggerView,nq}")]
    public sealed class FrameTimingPoint : ITimingPoint
    {
        private readonly IHostScreen _screen;
        private EventSource<FrameTimingPoint>? _event;
        private AsyncEventQueueCore _eventQueue;
        private readonly FrameTiming _timing;

        private CurrentFrameTiming CurrentTiming => _screen.CurrentTiming;

        public IHostScreen Screen => _screen;
        public FrameTiming TargetTiming => _timing;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerView => $"{_timing} ({_screen.GetType().Name} \"{_screen.Title}\")";

        internal FrameTimingPoint(IHostScreen screen, FrameTiming timing)
        {
            Debug.Assert(timing.IsSpecified());
            _screen = screen;
            _eventQueue = new AsyncEventQueueCore();
            _timing = timing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Event<FrameTimingPoint> AsEvent() => new(ref _event);

        public EventUnsubscriber<FrameTimingPoint> Subscribe(Action<FrameTimingPoint> action)
        {
            return AsEvent().Subscribe(action);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrameAsyncEnumerable Frames(CancellationToken cancellationToken = default)
        {
            return _screen.Frames(_timing, cancellationToken);
        }

        /// <summary>Switch to the next timing.</summary>
        /// <remarks>[NOTE] It is not necessarily the next frame.</remarks>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<AsyncUnit> Next(CancellationToken cancellationToken = default)
        {
            return FrameTimingAwaitableTaskSource.CreateTask(this, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<AsyncUnit> NextOrNow(CancellationToken cancellationToken = default)
        {
            if(CurrentTiming == _timing) {
                return new UniTask<AsyncUnit>(AsyncUnit.Default);
            }
            return Next(cancellationToken);
        }

        public async UniTask<AsyncUnit> NextFrame(CancellationToken cancellationToken = default)
        {
            var currentTiming = CurrentTiming;
            if(currentTiming == CurrentFrameTiming.OutOfFrameLoop) {
                return await Next(cancellationToken);
            }

            var endOfFrame = _screen.Timings.InternalEndOfFrame;
            await endOfFrame.NextOrNow(cancellationToken);
            return await Next(cancellationToken);
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
                await Next(cancellationToken);
            }
            return AsyncUnit.Default;
        }

        /// <summary>Wait for the specified time.</summary>
        /// <param name="time">time span for waiting</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        public async UniTask<AsyncUnit> DelayTime(TimeSpanF time, CancellationToken cancellationToken = default)
        {
            var start = _screen.Time;
            while(_screen.Time - start <= time) {
                await Next(cancellationToken);
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
            return DelayTime(TimeSpanF.FromMilliseconds(millisecond), cancellationToken);
        }

        /// <summary>Wait for the specified real time.</summary>
        /// <param name="time">time span for waiting</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        public async UniTask<AsyncUnit> DelayRealTime(TimeSpanF time, CancellationToken cancellationToken = default)
        {
            var start = Engine.RunningRealTime;
            while(Engine.RunningRealTime - start <= time) {
                await Next(cancellationToken);
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
            return DelayRealTime(TimeSpanF.FromMilliseconds(millisecond), cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AbortAllEvents()
        {
            _event?.Clear();
            _eventQueue.AbortAllEvents();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DoQueuedEvents()
        {
            _event?.Invoke(this);
            _eventQueue.DoQueuedEvents();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Post(Action continuation) => _eventQueue.Post(continuation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Post(Action<object?> continuation, object? state) => _eventQueue.Post(continuation, state);
    }
}
