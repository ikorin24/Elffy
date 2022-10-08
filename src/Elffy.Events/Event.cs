#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;

namespace Elffy
{
    [DebuggerDisplay("{DebuggerView,nq}")]
    public readonly ref struct Event<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ref EventSource<T>? _source;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerView
        {
            get
            {
                ref var source = ref _source;
                return Unsafe.IsNullRef(ref source)
                    ? $"{nameof(Event<T>)}<{typeof(T).Name}> (Never)"
                    : $"{nameof(Event<T>)}<{typeof(T).Name}> (Subscibed = {source?.SubscibedCount ?? 0})";
            }
        }

        public bool IsNever => Unsafe.IsNullRef(ref _source);

        public static Event<T> Never => default;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Event() => throw new NotSupportedException("Don't use default constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Event(ref EventSource<T>? source)
        {
            _source = ref source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventUnsubscriber<T> Subscribe(Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ref var source = ref _source;
            if(Unsafe.IsNullRef(ref source)) {
                return EventUnsubscriber<T>.None;
            }
            if(source is null) {
                Interlocked.CompareExchange(ref source, new EventSource<T>(), null);
            }
            source.Subscribe(action);
            return new EventUnsubscriber<T>(source, action);
        }

        public override bool Equals(object? obj) => false;

        [Obsolete($"GetHashCode() on {nameof(Event<T>)} will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new NotSupportedException($"GetHashCode() on {nameof(Event<T>)} is not supported.");
    }
}
