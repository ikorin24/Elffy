#nullable enable
using System;
using System.ComponentModel;

namespace Elffy
{
    public readonly struct EventUnsubscriber<T> : IDisposable, IEquatable<EventUnsubscriber<T>>
    {
        private readonly EventSource<T>? _source;
        private readonly Action<T>? _action;

        public static EventUnsubscriber<T> None => default;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EventUnsubscriber() => throw new NotSupportedException("Don't use default constructor.");

        internal EventUnsubscriber(EventSource<T>? source, Action<T> action)
        {
            _source = source;
            _action = action;
        }

        public void Dispose()
        {
            _source?.Unsubscribe(_action);
        }

        internal (EventSource<T>? Source, Action<T>? Action) GetInnerValues()
        {
            return (_source, _action);
        }

        public override bool Equals(object? obj) => obj is EventUnsubscriber<T> unsubscriber && Equals(unsubscriber);

        public bool Equals(EventUnsubscriber<T> other) => _source == other._source && _action == other._action;

        public override int GetHashCode() => HashCode.Combine(_source, _action);
    }
}
