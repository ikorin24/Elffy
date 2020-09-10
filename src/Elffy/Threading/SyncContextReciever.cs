#nullable enable
using System.Collections.Concurrent;
using System.Threading;

namespace Elffy.Threading
{
    internal class SyncContextReceiver
    {
        private readonly ConcurrentQueue<(SendOrPostCallback callback, object? state)> _queue
            = new ConcurrentQueue<(SendOrPostCallback callback, object? state)>();

        public void Add(SendOrPostCallback callback, object? state) => _queue.Enqueue((callback, state));

        public void DoAll()
        {
            var count = _queue.Count;
            if(count == 0) { return; }
            Do(count, _queue);
            
            static void Do(int count, ConcurrentQueue<(SendOrPostCallback callback, object? state)> queue)
            {
                while(count > 0 && queue.TryDequeue(out var item)) {
                    item.callback(item.state);
                    count--;
                }
            }
        }
    }
}
