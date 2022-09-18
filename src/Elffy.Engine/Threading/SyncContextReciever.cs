#nullable enable
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.ComponentModel;

namespace Elffy.Threading
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class SyncContextReceiver
    {
        private readonly ConcurrentQueue<(SendOrPostCallback callback, object? state)> _queue
            = new ConcurrentQueue<(SendOrPostCallback callback, object? state)>();

        internal SyncContextReceiver()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Add(SendOrPostCallback callback, object? state) => _queue.Enqueue((callback, state));

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
                        if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                        // Don't throw. (Ignore exceptions in user code)
                    }
                    count--;
                }
            }
        }
    }
}
