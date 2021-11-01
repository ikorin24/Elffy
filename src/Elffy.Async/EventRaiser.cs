#nullable enable
using System;
using System.Diagnostics;

namespace Elffy
{
    public sealed class EventRaiser<T>
    {
        private int _count;
        public int SubscibedCount => _count;

        internal void Subscribe(Action<T> action)
        {
            Debug.Assert(action is not null);
            throw new NotImplementedException();
        }

        internal void Unsubscribe(Action<T>? action)
        {
            throw new NotImplementedException();
        }
    }
}
