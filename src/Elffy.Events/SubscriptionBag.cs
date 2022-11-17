#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public SubscriptionRegister Register => new SubscriptionRegister(this);

        public SubscriptionBag()
        {
        }

        internal unsafe void Add<T>(EventSubscription<T> subscription)
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
                var source = SafeCast.As<EventHandlerHolder<T>>(s);
                var func = SafeCast.As<Action<T>>(f);
                source.Unsubscribe(func);
            }
        }

        internal unsafe void Add<T>(AsyncEventSubscription<T> subscription)
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
                var source = SafeCast.As<AsyncEventHandlerHolder<T>>(s);
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

    public readonly struct SubscriptionRegister
    {
        private readonly SubscriptionBag? _bag;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't use default constructor.", true)]
        public SubscriptionRegister() => throw new NotSupportedException("Don't use default constructor.");

        internal SubscriptionRegister(SubscriptionBag bag) => _bag = bag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(EventSubscription<T> subscription)
        {
            _bag?.Add(subscription);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(AsyncEventSubscription<T> subscription)
        {
            _bag?.Add(subscription);
        }
    }
}
