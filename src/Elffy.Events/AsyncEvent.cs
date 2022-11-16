#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;

namespace Elffy
{
    [DebuggerDisplay("{DebuggerView,nq}")]
    public readonly ref struct AsyncEvent<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ref AsyncEventHandlerHolder<T>? _source;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerView
        {
            get
            {
                ref var source = ref _source;
                return Unsafe.IsNullRef(ref source)
                    ? $"{nameof(AsyncEvent<T>)}<{typeof(T).Name}> (Never)"
                    : $"{nameof(AsyncEvent<T>)}<{typeof(T).Name}> (Subscibed = {source?.SubscibedCount ?? 0})";
            }
        }

        public bool IsNever => Unsafe.IsNullRef(ref _source);

        public static AsyncEvent<T> Never => default;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public AsyncEvent() => throw new NotSupportedException("Don't use default constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal AsyncEvent(ref AsyncEventHandlerHolder<T>? source)
        {
            _source = ref source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEventSubscription<T> Subscribe(Func<T, CancellationToken, UniTask> func)
        {
            ArgumentNullException.ThrowIfNull(func);
            ref var source = ref _source;
            if(Unsafe.IsNullRef(ref source)) {
                return AsyncEventSubscription<T>.None;
            }
            if(source is null) {
                Interlocked.CompareExchange(ref source, new AsyncEventHandlerHolder<T>(), null);
            }
            source.Subscribe(func);
            return new AsyncEventSubscription<T>(source, func);
        }

        public override bool Equals(object? obj) => false;

        [Obsolete($"GetHashCode() on {nameof(AsyncEvent<T>)} will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new NotSupportedException($"GetHashCode() on {nameof(AsyncEvent<T>)} is not supported.");
    }
}
