#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy
{
    public readonly struct AsyncEventUnsubscriber : IDisposable, IEquatable<AsyncEventUnsubscriber>
    {
        private readonly AsyncEventRaiser? _raiser;
        private readonly Func<CancellationToken, UniTask>? _func;

        public static AsyncEventUnsubscriber None => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal AsyncEventUnsubscriber(AsyncEventRaiser? raiser, Func<CancellationToken, UniTask>? func)
        {
            _raiser = raiser;
            _func = func;
        }

        public void Dispose()
        {
            _raiser?.Unsubscribe(_func);
        }

        public override bool Equals(object? obj) => obj is AsyncEventUnsubscriber unsubscriber && Equals(unsubscriber);

        public bool Equals(AsyncEventUnsubscriber other) => _raiser == other._raiser && _func == other._func;

        public override int GetHashCode() => HashCode.Combine(_raiser, _func);
    }
}
