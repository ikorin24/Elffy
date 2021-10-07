#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Features.Internal
{
    internal readonly struct AsyncEventQueueCore
    {
        private readonly ConcurrentQueue<WorkItem> _queue;

        private AsyncEventQueueCore(ConcurrentQueue<WorkItem> queue)
        {
            _queue = queue;
        }

        public static AsyncEventQueueCore New() => new AsyncEventQueueCore(new ConcurrentQueue<WorkItem>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Post(Action continuation)
        {
            if(continuation is null) { return; }
            _queue.Enqueue(new WorkItem(continuation));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Post(Action<object?> continuation, object? state)
        {
            if(continuation is null) { return; }
            _queue.Enqueue(new WorkItem(continuation, state));
        }

        public void AbortAllEvents()
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
