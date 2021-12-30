#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy
{
    public readonly struct AsyncEventUnsubscriber<T> : IDisposable, IEquatable<AsyncEventUnsubscriber<T>>
    {
        private readonly AsyncEventRaiser<T>? _raiser;
        private readonly Func<T, CancellationToken, UniTask>? _func;

        public static AsyncEventUnsubscriber<T> None => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal AsyncEventUnsubscriber(AsyncEventRaiser<T>? raiser, Func<T, CancellationToken, UniTask>? func)
        {
            _raiser = raiser;
            _func = func;
        }

        public void Dispose()
        {
            _raiser?.Unsubscribe(_func);
        }

        internal (AsyncEventRaiser<T>? Raiser, Func<T, CancellationToken, UniTask>? Func) GetInnerValues()
        {
            return (_raiser, _func);
        }

        public override bool Equals(object? obj) => obj is AsyncEventUnsubscriber<T> unsubscriber && Equals(unsubscriber);

        public bool Equals(AsyncEventUnsubscriber<T> other) => _raiser == other._raiser && _func == other._func;

        public override int GetHashCode() => HashCode.Combine(_raiser, _func);
    }
}
