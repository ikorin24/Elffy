#nullable enable
using System.Collections.Concurrent;
using System.Threading;

namespace Elffy.Threading
{
    internal class SyncContextReceiver
    {
        private static readonly ConcurrentQueue<(SendOrPostCallback callback, object? state)> _queue
            = new ConcurrentQueue<(SendOrPostCallback callback, object? state)>();

        public void Add(SendOrPostCallback callback, object? state) => _queue.Enqueue((callback, state));

        public void DoAll()
        {
            var count = _queue.Count;
            while(count > 0 && _queue.TryDequeue(out var item)) {
                item.callback(item.state);
                count--;
            }
        }
    }
}
