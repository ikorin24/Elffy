#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy
{
    public sealed class AsyncBackEndPoint
    {
        private readonly ConcurrentDictionary<FrameLoopTiming, ConcurrentQueue<WorkItem>> _queues;

        internal AsyncBackEndPoint()
        {
            _queues = new();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrameLoopAwaitable ToFrameLoopEvent(FrameLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return new FrameLoopAwaitable(this, timing, cancellationToken);
        }

        /// <summary>Abort all suspended tasks by clearing the queue.</summary>
        internal void AbortAll()
        {
            _queues.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Post(FrameLoopTiming timing, Action continuation)
        {
            if(continuation is null) { return; }
            if(!_queues.TryGetValue(timing, out var queue)) {
                queue = InitQueue(timing);
            }
            queue.Enqueue(new WorkItem(continuation));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Post(FrameLoopTiming timing, Action<object?> continuation, object? state)
        {
            if(continuation is null) { return; }
            if(!_queues.TryGetValue(timing, out var queue)) {
                queue = InitQueue(timing);
            }
            queue.Enqueue(new WorkItem(continuation, state));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DoQueuedEvents(FrameLoopTiming eventType)
        {
            int count;
            if(_queues.TryGetValue(eventType, out var queue) && (count = queue.Count) > 0) {
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
