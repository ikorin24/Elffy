#nullable enable
using System;
using System.ComponentModel;

namespace Elffy
{
    public readonly struct EventSubscription<T> : IDisposable, IEquatable<EventSubscription<T>>
    {
        private readonly EventHandlerHolder<T>? _source;
        private readonly Action<T>? _action;

        public static EventSubscription<T> None => default;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EventSubscription() => throw new NotSupportedException("Don't use default constructor.");

        internal EventSubscription(EventHandlerHolder<T>? source, Action<T> action)
        {
            _source = source;
            _action = action;
        }

        public void Dispose()
        {
            _source?.Unsubscribe(_action);
        }

        internal (EventHandlerHolder<T>? Source, Action<T>? Action) GetInnerValues()
        {
            return (_source, _action);
        }

        public override bool Equals(object? obj) => obj is EventSubscription<T> unsubscriber && Equals(unsubscriber);

        public bool Equals(EventSubscription<T> other) => _source == other._source && _action == other._action;

        public override int GetHashCode() => HashCode.Combine(_source, _action);
    }
}
