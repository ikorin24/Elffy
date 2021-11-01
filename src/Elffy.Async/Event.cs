#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace Elffy
{
    public readonly ref struct Event<T>
    {
        // [NOTE]
        // Use Span<T> as a ByReference<T>.
        // For now, ByReference<T> is not yet available in the user library.
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Span<EventRaiser<T>?> _raiserRef;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerView => $"{nameof(Event<T>)}<{nameof(T)}> (Subscibed = {Raiser?.SubscibedCount ?? 0})";

        private readonly ref EventRaiser<T>? Raiser
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.GetReference(_raiserRef);
        }

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
            if(action is null) {
                ThrowNullArg(nameof(action));
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

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);
    }
}
