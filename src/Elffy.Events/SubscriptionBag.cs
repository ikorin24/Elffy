#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Threading;

namespace Elffy
{
    public sealed class SubscriptionBag : IDisposable
    {
        private List<BagItem>? _list;
        private FastSpinLock _lock;
        private bool _disposed;

        public SubscriptionBag()
        {
        }

        public unsafe void Add<T>(EventSubscription<T> subscription)
        {
            var (source, func) = subscription.GetInnerValues();
            if(source == null || func == null) { return; }
            _lock.Enter();
            try {
                if(_disposed) {
                    source.Unsubscribe(func);
                    return;
                }
                _list ??= new();
                _list.Add(new BagItem(&OnDispose, source, func));
            }
            finally {
                _lock.Exit();
            }

            static void OnDispose(object s, Delegate f)
            {
                var source = SafeCast.As<EventSource<T>>(s);
                var func = SafeCast.As<Action<T>>(f);
                source.Unsubscribe(func);
            }
        }

        public unsafe void Add<T>(AsyncEventSubscription<T> subscription)
        {
            var (source, func) = subscription.GetInnerValues();
            if(source == null || func == null) { return; }
            _lock.Enter();
            try {
                if(_disposed) {
                    source.Unsubscribe(func);
                    return;
                }
                _list ??= new();
                _list.Add(new BagItem(&OnDispose, source, func));
            }
            finally {
                _lock.Exit();
            }

            static void OnDispose(object s, Delegate f)
            {
                var source = SafeCast.As<AsyncEventSource<T>>(s);
                var func = SafeCast.As<Func<T, CancellationToken, UniTask>>(f);
                source.Unsubscribe(func);
            }
        }

        public void Dispose()
        {
            _lock.Enter();
            try {
                if(_disposed) {
                    return;
                }
                _disposed = true;
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
            private readonly object _o;
            private readonly Delegate _delegate;

            public BagItem(delegate*<object, Delegate, void> p, object o, Delegate del)
            {
                _p = p;
                _o = o;
                _delegate = del;
            }

            public void Invoke() => _p(_o, _delegate);
        }
    }

    public static class SubscriptionBagExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddTo<T>(this EventSubscription<T> unsubscriber, SubscriptionBag bag)
        {
            ArgumentNullException.ThrowIfNull(bag);
            bag.Add(unsubscriber);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddTo<T>(this AsyncEventSubscription<T> unsubscriber, SubscriptionBag bag)
        {
            ArgumentNullException.ThrowIfNull(bag);
            bag.Add(unsubscriber);
        }
    }
}
