#nullable enable
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy.Threading
{
    internal sealed class SyncContextReceiver
    {
        private readonly ConcurrentQueue<(SendOrPostCallback callback, object? state)> _queue
            = new ConcurrentQueue<(SendOrPostCallback callback, object? state)>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(SendOrPostCallback callback, object? state) => _queue.Enqueue((callback, state));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoAll()
        {
            var count = _queue.Count;
            if(count == 0) { return; }
            Do(count, _queue);
            
            static void Do(int count, ConcurrentQueue<(SendOrPostCallback callback, object? state)> queue)
            {
                while(count > 0 && queue.TryDequeue(out var item)) {
                    try {
                        item.callback(item.state);
                    }
                    catch {
                        // Don't throw. (Ignore exceptions in user code)
                    }
                    count--;
                }
            }
        }
    }
}
