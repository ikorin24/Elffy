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
    [DebuggerDisplay("{DebuggerView,nq}")]
    public readonly ref struct AsyncEvent<T>
    {
        // [NOTE]
        // Use Span<T> as a ByReference<T>.
        // For now, ByReference<T> is not yet available in the user library.
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Span<AsyncEventRaiser<T>?> _raiserRef;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerView => $"{nameof(AsyncEvent<T>)}<{nameof(T)}> (Subscibed = {Raiser?.SubscibedCount ?? 0})";

        private readonly ref AsyncEventRaiser<T>? Raiser
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.GetReference(_raiserRef);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEvent(ref AsyncEventRaiser<T>? raiser)
        {
            _raiserRef = MemoryMarshal.CreateSpan(ref raiser, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncEvent<T> FromRaiser(ref AsyncEventRaiser<T>? raiser)
        {
            return new AsyncEvent<T>(ref raiser);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEventUnsubscriber<T> Subscribe(Func<T, CancellationToken, UniTask> func)
        {
            if(func is null) {
                ThrowNullArg(nameof(func));
            }
            ref var raiser = ref Raiser;
            if(raiser is null) {
                CreateRaiser(ref raiser);
            }
            raiser.Subscribe(func);
            return new AsyncEventUnsubscriber<T>(raiser, func);
        }

        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => Raiser?.GetHashCode() ?? 0;

        public static bool operator ==(AsyncEvent<T> left, AsyncEvent<T> right) => left.Raiser == right.Raiser;

        public static bool operator !=(AsyncEvent<T> left, AsyncEvent<T> right) => !(left == right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateRaiser([AllowNull] ref AsyncEventRaiser<T> raiser)
        {
            Interlocked.CompareExchange(ref raiser, new AsyncEventRaiser<T>(), null);
        }

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);
    }
}
