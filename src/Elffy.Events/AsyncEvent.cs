#nullable enable
using Cysharp.Threading.Tasks;
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
    public readonly ref struct AsyncEvent<T>
    {
        // [NOTE]
        // Use Span<T> as a ByReference<T>.
        // For now, ByReference<T> is not yet available in the user library.
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Span<AsyncEventSource<T>?> _sourceRef;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerView
        {
            get
            {
                if(IsNever) {
                    return $"{nameof(AsyncEvent<T>)}<{typeof(T).Name}> (Never)";
                }
                else {
                    return $"{nameof(AsyncEvent<T>)}<{typeof(T).Name}> (Subscibed = {Source?.SubscibedCount ?? 0})";
                }
            }
        }

        private ref AsyncEventSource<T>? Source
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.GetReference(_sourceRef);
        }

        public bool IsNever => Unsafe.IsNullRef(ref Source);

        public static AsyncEvent<T> Never => default;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public AsyncEvent() => throw new NotSupportedException("Don't use default constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEvent([AllowNull] ref AsyncEventSource<T> source)
        {
            _sourceRef = MemoryMarshal.CreateSpan(ref source!, 1)!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncEvent<T> FromSource([AllowNull] ref AsyncEventSource<T> source)
        {
            return new AsyncEvent<T>(ref source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEventUnsubscriber<T> Subscribe(Func<T, CancellationToken, UniTask> func)
        {
            ArgumentNullException.ThrowIfNull(func);
            if(IsNever) {
                return AsyncEventUnsubscriber<T>.None;
            }
            ref var source = ref Source;
            if(source is null) {
                CreateSource(ref source);
            }
            source.Subscribe(func);
            return new AsyncEventUnsubscriber<T>(source, func);
        }

        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => Source?.GetHashCode() ?? 0;

        public static bool operator ==(AsyncEvent<T> left, AsyncEvent<T> right) => left.Source == right.Source;

        public static bool operator !=(AsyncEvent<T> left, AsyncEvent<T> right) => !(left == right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateSource([AllowNull] ref AsyncEventSource<T> source)
        {
            Interlocked.CompareExchange(ref source, new AsyncEventSource<T>(), null);
        }
    }
}
