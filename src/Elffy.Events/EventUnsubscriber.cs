#nullable enable
using System;
using System.ComponentModel;

namespace Elffy
{
    public readonly struct EventUnsubscriber<T> : IDisposable, IEquatable<EventUnsubscriber<T>>
    {
        private readonly EventRaiser<T>? _raiser;
        private readonly Action<T>? _action;

        public static EventUnsubscriber<T> None => default;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EventUnsubscriber() => throw new NotSupportedException("Don't use default constructor.");

        internal EventUnsubscriber(EventRaiser<T>? raiser, Action<T> action)
        {
            _raiser = raiser;
            _action = action;
        }

        public void Dispose()
        {
            _raiser?.Unsubscribe(_action);
        }

        internal (EventRaiser<T>? Raiser, Action<T>? Action) GetInnerValues()
        {
            return (_raiser, _action);
        }

        public override bool Equals(object? obj) => obj is EventUnsubscriber<T> unsubscriber && Equals(unsubscriber);

        public bool Equals(EventUnsubscriber<T> other) => _raiser == other._raiser && _action == other._action;

        public override int GetHashCode() => HashCode.Combine(_raiser, _action);
    }
}
