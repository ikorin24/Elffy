#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;

namespace Elffy
{
    [DebuggerDisplay("{DebuggerView,nq}")]
    public readonly ref struct Event<T>
    {
        // [NOTE]
        // Use Span<T> as a ByReference<T>.
        // For now, ByReference<T> is not yet available in the user library.
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Span<EventSource<T>?> _sourceRef;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerView
        {
            get
            {
                if(IsNever) {
                    return $"{nameof(Event<T>)}<{typeof(T).Name}> (Never)";
                }
                else {
                    return $"{nameof(Event<T>)}<{typeof(T).Name}> (Subscibed = {Source?.SubscibedCount ?? 0})";
                }
            }
        }

        private ref EventSource<T>? Source
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.GetReference(_sourceRef);
        }

        public bool IsNever => Unsafe.IsNullRef(ref Source);

        public static Event<T> Never => default;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Event() => throw new NotSupportedException("Don't use default constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Event(ref EventSource<T>? source)
        {
            _sourceRef = MemoryMarshal.CreateSpan(ref source, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Event<T> FromSource(ref EventSource<T>? source)
        {
            return new Event<T>(ref source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventUnsubscriber<T> Subscribe(Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if(IsNever) {
                return EventUnsubscriber<T>.None;
            }
            ref var source = ref Source;
            if(source is null) {
                CreateSource(ref source);
            }
            source.Subscribe(action);
            return new EventUnsubscriber<T>(source, action);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateSource([AllowNull] ref EventSource<T> source)
        {
            Interlocked.CompareExchange(ref source, new EventSource<T>(), null);
        }
    }
}
