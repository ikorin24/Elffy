#nullable enable
using System;
using System.Collections.Concurrent;

namespace Elffy.Core
{
    internal class SyncContextReceiver
    {
        private static readonly ConcurrentQueue<Action> _invokedActions = new ConcurrentQueue<Action>();

        public void Add(Action action) => _invokedActions.Enqueue(action);

        public void DoAll()
        {
            var count = _invokedActions.Count;
            while(count > 0 && _invokedActions.TryDequeue(out var action)) {
                action();
                count--;
            }
        }
    }
}
