#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy
{
    public readonly struct AsyncEventUnsubscriber<T> : IDisposable, IEquatable<AsyncEventUnsubscriber<T>>
    {
        private readonly AsyncEventSource<T>? _source;
        private readonly Func<T, CancellationToken, UniTask>? _func;

        public static AsyncEventUnsubscriber<T> None => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal AsyncEventUnsubscriber(AsyncEventSource<T>? source, Func<T, CancellationToken, UniTask>? func)
        {
            _source = source;
            _func = func;
        }

        public void Dispose()
        {
            _source?.Unsubscribe(_func);
        }

        internal (AsyncEventSource<T>? Source, Func<T, CancellationToken, UniTask>? Func) GetInnerValues()
        {
            return (_source, _func);
        }

        public override bool Equals(object? obj) => obj is AsyncEventUnsubscriber<T> unsubscriber && Equals(unsubscriber);

        public bool Equals(AsyncEventUnsubscriber<T> other) => _source == other._source && _func == other._func;

        public override int GetHashCode() => HashCode.Combine(_source, _func);
    }
}
