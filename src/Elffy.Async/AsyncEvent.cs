#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace Elffy
{
    public readonly ref struct AsyncEvent
    {
        // [NOTE]
        // Use Span<T> as a ByReference<T>.
        // For now, ByReference<T> is not yet available in the user library.
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Span<AsyncEventRaiser?> _raiserRef;

        private readonly ref AsyncEventRaiser? Raiser
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.GetReference(_raiserRef);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEvent(ref AsyncEventRaiser? raiser)
        {
            _raiserRef = MemoryMarshal.CreateSpan(ref raiser, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncEvent FromRaiser(ref AsyncEventRaiser? raiser)
        {
            return new AsyncEvent(ref raiser);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEventUnsubscriber Subscribe(Func<CancellationToken, UniTask> func)
        {
            if(func is null) {
                ThrowNullArg(nameof(func));
            }
            ref var raiser = ref Raiser;
            if(raiser is null) {
                CreateRaiser(ref raiser);
            }
            raiser.Subscribe(func);
            return new AsyncEventUnsubscriber(raiser, func);
        }

        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => Raiser?.GetHashCode() ?? 0;

        public static bool operator ==(AsyncEvent left, AsyncEvent right) => left.Raiser == right.Raiser;

        public static bool operator !=(AsyncEvent left, AsyncEvent right) => !(left == right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateRaiser([AllowNull] ref AsyncEventRaiser raiser)
        {
            Interlocked.CompareExchange(ref raiser, new AsyncEventRaiser(), null);
        }

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);
    }
}
