#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public interface IReactive<TSelf>
    {
        Event<ReactiveState<TSelf>> PropertyChanged { get; }
    }

    public static class ReactiveExtensions
    {
        public static EventSubscription<ReactiveState<T>> Subscribe<T>(this Event<ReactiveState<T>> @event, string propertyName, Action<T> action)
        {
            // [capture] propertyName, action
            return @event.Subscribe(state =>
            {
                if(state.PropertyName != propertyName) { return; }
                action.Invoke(state.Sender);
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invoke<T>(this EventSource<ReactiveState<T>> eventSource, T sender, string propertyName)
        {
            var state = new ReactiveState<T>(sender, propertyName);
            eventSource.Invoke(state);
        }
    }

    public readonly struct ReactiveState<T> : IEquatable<ReactiveState<T>>
    {
        private readonly T _sender;
        private readonly string _propertyName;

        public T Sender => _sender;
        public string PropertyName => _propertyName;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReactiveState(T sender, string propertyName)
        {
            ArgumentException.ThrowIfNullOrEmpty(propertyName);
            _sender = sender;
            _propertyName = propertyName;
        }

        public override bool Equals(object? obj) => obj is ReactiveState<T> state && Equals(state);

        public bool Equals(ReactiveState<T> other)
        {
            return EqualityComparer<T>.Default.Equals(_sender, other._sender) &&
                   _propertyName == other._propertyName;
        }

        public override int GetHashCode() => HashCode.Combine(_sender, _propertyName);

        public static bool operator ==(ReactiveState<T> left, ReactiveState<T> right) => left.Equals(right);

        public static bool operator !=(ReactiveState<T> left, ReactiveState<T> right) => !(left == right);
    }
}
