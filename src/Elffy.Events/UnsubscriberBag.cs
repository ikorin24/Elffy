#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Threading;

namespace Elffy
{
    public sealed class UnsubscriberBag : IDisposable
    {
        private List<BagItem>? _list;
        private FastSpinLock _lock;

        public unsafe void Add<T>(EventUnsubscriber<T> unsubscriber)
        {
            var (raiser, func) = unsubscriber.GetInnerValues();
            if(raiser == null || func == null) { return; }
            _lock.Enter();
            try {
                _list ??= new();
                _list.Add(new BagItem(&OnDispose, raiser, func));
            }
            finally {
                _lock.Exit();
            }

            static void OnDispose(object r, Delegate f)
            {
                var raiser = SafeCast.As<EventRaiser<T>>(r);
                var func = SafeCast.As<Action<T>>(f);
                raiser.Unsubscribe(func);
            }
        }

        public unsafe void Add<T>(AsyncEventUnsubscriber<T> unsubscriber)
        {
            var (raiser, func) = unsubscriber.GetInnerValues();
            if(raiser == null || func == null) { return; }
            _lock.Enter();
            try {
                _list ??= new();
                _list.Add(new BagItem(&OnDispose, raiser, func));
            }
            finally {
                _lock.Exit();
            }

            static void OnDispose(object r, Delegate f)
            {
                var raiser = SafeCast.As<AsyncEventRaiser<T>>(r);
                var func = SafeCast.As<Func<T, CancellationToken, UniTask>>(f);
                raiser.Unsubscribe(func);
            }
        }

        public void Dispose()
        {
            _lock.Enter();
            try {
                var list = _list;
                if(list is null) { return; }
                foreach(var item in list.AsReadOnlySpan()) {
                    item.Invoke();
                }
                list.Clear();
            }
            finally {
                _lock.Exit();
            }
        }

        private unsafe readonly struct BagItem
        {
            private readonly delegate*<object, Delegate, void> _p;
            private readonly object _raiser;
            private readonly Delegate _delegate;

            public BagItem(delegate*<object, Delegate, void> p, object raiser, Delegate del)
            {
                _p = p;
                _raiser = raiser;
                _delegate = del;
            }

            public void Invoke() => _p(_raiser, _delegate);
        }
    }

    public static class UnsubscriberBagExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddTo<T>(this EventUnsubscriber<T> unsubscriber, UnsubscriberBag bag)
        {
            if(bag is null) { ThrowNullArg(nameof(bag)); }
            bag.Add(unsubscriber);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddTo<T>(this AsyncEventUnsubscriber<T> unsubscriber, UnsubscriberBag bag)
        {
            if(bag is null) { ThrowNullArg(nameof(bag)); }
            bag.Add(unsubscriber);
        }

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);
    }
}
