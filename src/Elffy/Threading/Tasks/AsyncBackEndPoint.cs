#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Elffy.Threading.Tasks
{
    public sealed class AsyncBackEndPoint
    {
        private readonly Dictionary<FrameLoopTiming, ConcurrentQueue<Action>> _queues;

        internal AsyncBackEndPoint()
        {
            _queues = new Dictionary<FrameLoopTiming, ConcurrentQueue<Action>>();
        }

        public FrameLoopAwaitable ToFrameLoopEvent(FrameLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return new FrameLoopAwaitable(this, timing, cancellationToken);
        }


        internal void Post(FrameLoopTiming eventType, Action continuation)
        {
            if(!_queues.TryGetValue(eventType, out var queue)) {
                InitQueue(eventType);

                void InitQueue(FrameLoopTiming eventType)
                {
                    lock(_queues) {
                        _queues.TryAdd(eventType, new ConcurrentQueue<Action>());
                    }
                }
            }
            _queues[eventType].Enqueue(continuation);
        }

        internal void DoQueuedEvents(FrameLoopTiming eventType)
        {
            int count;
            if(_queues.TryGetValue(eventType, out var queue) && (count = queue.Count) > 0) {
                Do(queue, count);

                static void Do(ConcurrentQueue<Action> queue, int count)
                {
                    for(int i = 0; i < count; i++) {
                        var exists = queue.TryDequeue(out var action);
                        Debug.Assert(exists);
                        action!();
                    }
                }
            }
            return;
        }
    }
}
