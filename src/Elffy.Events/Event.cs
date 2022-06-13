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
        private readonly Span<EventRaiser<T>?> _raiserRef;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerView
        {
            get
            {
                if(IsNever) {
                    return $"{nameof(Event<T>)}<{typeof(T).Name}> (Never)";
                }
                else {
                    return $"{nameof(Event<T>)}<{typeof(T).Name}> (Subscibed = {Raiser?.SubscibedCount ?? 0})";
                }
            }
        }

        private ref EventRaiser<T>? Raiser
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.GetReference(_raiserRef);
        }

        public bool IsNever => Unsafe.IsNullRef(ref Raiser);

        public static Event<T> Never => default;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Event() => throw new NotSupportedException("Don't use default constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Event(ref EventRaiser<T>? raiser)
        {
            _raiserRef = MemoryMarshal.CreateSpan(ref raiser, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Event<T> FromRaiser(ref EventRaiser<T>? raiser)
        {
            return new Event<T>(ref raiser);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventUnsubscriber<T> Subscribe(Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if(IsNever) {
                return EventUnsubscriber<T>.None;
            }
            ref var raiser = ref Raiser;
            if(raiser is null) {
                CreateRaiser(ref raiser);
            }
            raiser.Subscribe(action);
            return new EventUnsubscriber<T>(raiser, action);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateRaiser([AllowNull] ref EventRaiser<T> raiser)
        {
            Interlocked.CompareExchange(ref raiser, new EventRaiser<T>(), null);
        }
    }
}
