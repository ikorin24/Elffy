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

        public UnsubscriberBag()
        {
        }

        public unsafe void Add<T>(EventUnsubscriber<T> unsubscriber)
        {
            var (source, func) = unsubscriber.GetInnerValues();
            if(source == null || func == null) { return; }
            _lock.Enter();
            try {
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

        public unsafe void Add<T>(AsyncEventUnsubscriber<T> unsubscriber)
        {
            var (source, func) = unsubscriber.GetInnerValues();
            if(source == null || func == null) { return; }
            _lock.Enter();
            try {
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

    public static class UnsubscriberBagExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddTo<T>(this EventUnsubscriber<T> unsubscriber, UnsubscriberBag bag)
        {
            ArgumentNullException.ThrowIfNull(bag);
            bag.Add(unsubscriber);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddTo<T>(this AsyncEventUnsubscriber<T> unsubscriber, UnsubscriberBag bag)
        {
            ArgumentNullException.ThrowIfNull(bag);
            bag.Add(unsubscriber);
        }
    }
}
