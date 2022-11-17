#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy
{
    public readonly struct AsyncEventSubscription<T> : IDisposable, IEquatable<AsyncEventSubscription<T>>
    {
        private readonly AsyncEventHandlerHolder<T>? _source;
        private readonly Func<T, CancellationToken, UniTask>? _func;

        public static AsyncEventSubscription<T> None => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal AsyncEventSubscription(AsyncEventHandlerHolder<T>? source, Func<T, CancellationToken, UniTask>? func)
        {
            _source = source;
            _func = func;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTo(SubscriptionBag bag)
        {
            ArgumentNullException.ThrowIfNull(bag);
            bag.Add(this);
        }

        public void Dispose()
        {
            _source?.Unsubscribe(_func);
        }

        internal (AsyncEventHandlerHolder<T>? Source, Func<T, CancellationToken, UniTask>? Func) GetInnerValues()
        {
            return (_source, _func);
        }

        public override bool Equals(object? obj) => obj is AsyncEventSubscription<T> unsubscriber && Equals(unsubscriber);

        public bool Equals(AsyncEventSubscription<T> other) => _source == other._source && _func == other._func;

        public override int GetHashCode() => HashCode.Combine(_source, _func);
    }
}
