#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Elffy
{
    public sealed class AsyncBackEndPoint
    {
        private readonly IHostScreen _screen;
        private readonly ConcurrentDictionary<FrameLoopTiming, ConcurrentQueue<WorkItem>> _queues;

        internal IHostScreen Screen => _screen;

        /// <summary>Get current screen frame loop timing.</summary>
        /// <remarks>If not main thread of <see cref="IHostScreen"/>, always returns <see cref="ScreenCurrentTiming.OutOfFrameLoop"/></remarks>
        public ScreenCurrentTiming CurrentTiming => _screen.CurrentTiming;

        internal AsyncBackEndPoint(IHostScreen screen)
        {
            _screen = screen;
            _queues = new();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrameLoopAwaitable ToTiming(FrameLoopTiming timing, CancellationToken cancellationToken = default)
        {
            timing.ThrowArgExceptionIfInvalidExceptNone(nameof(timing));
            return new FrameLoopAwaitable(this, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask Ensure(FrameLoopTiming timing, CancellationToken cancellation = default)
        {
            if(CurrentTiming.TimingEquals(timing)) {
                return UniTask.CompletedTask;
            }
            return ToTiming(timing, cancellation).AsUniTask();
        }

        /// <summary>Abort all suspended tasks by clearing the queue.</summary>
        internal void AbortAll()
        {
            _queues.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Post(FrameLoopTiming timing, Action continuation)
        {
            Debug.Assert(timing.IsValid());
            if(continuation is null) { return; }
            if(!_queues.TryGetValue(timing, out var queue)) {
                queue = InitQueue(timing);
            }
            queue.Enqueue(new WorkItem(continuation));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Post(FrameLoopTiming timing, Action<object?> continuation, object? state)
        {
            Debug.Assert(timing.IsValid());
            if(continuation is null) { return; }
            if(!_queues.TryGetValue(timing, out var queue)) {
                queue = InitQueue(timing);
            }
            queue.Enqueue(new WorkItem(continuation, state));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DoQueuedEvents(FrameLoopTiming timing)
        {
            Debug.Assert(timing.IsValid());
            int count;
            if(_queues.TryGetValue(timing, out var queue) && (count = queue.Count) > 0) {
                Do(queue, count);

                [MethodImpl(MethodImplOptions.NoInlining)]
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


        [MethodImpl(MethodImplOptions.NoInlining)]
        private ConcurrentQueue<WorkItem> InitQueue(FrameLoopTiming eventType)
        {
            return _queues.GetOrAdd(eventType, _ => new());
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
                _action = state => SafeCast.As<Action>(state!).Invoke();
                _state = action;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Invoke()
            {
                _action.Invoke(_state);
            }
        }
    }
}
