#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Threading;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CA1068 // CancellationToken parameters must come last

namespace Elffy
{
    public struct AsyncEventSource<T> : IEquatable<AsyncEventSource<T>>
    {
        private AsyncEventHandlerHolder<T>? _source;

        public static AsyncEventSource<T> Default => default;

        [UnscopedRef]
        public AsyncEvent<T> Event => new AsyncEvent<T>(ref _source);

        public readonly int SubscribedCount => _source?.SubscribedCount ?? 0;

        public readonly UniTask Invoke(T arg, CancellationToken cancellationToken = default)
        {
            var source = _source;
            if(source is not null) {
                return source.Invoke(arg, cancellationToken);
            }
            else {
                if(cancellationToken.IsCancellationRequested) {
                    return UniTask.FromCanceled(cancellationToken);
                }
                else {
                    return UniTask.CompletedTask;
                }
            }
        }

        public readonly UniTask InvokeSequentially(T arg, CancellationToken cancellationToken = default)
        {
            var source = _source;
            if(source is not null) {
                return source.InvokeSequentially(arg, cancellationToken);
            }
            else {
                if(cancellationToken.IsCancellationRequested) {
                    return UniTask.FromCanceled(cancellationToken);
                }
                else {
                    return UniTask.CompletedTask;
                }
            }
        }

        public readonly void Clear() => _source?.Clear();

        public Func<T, CancellationToken, UniTask> ToSequentialDelegate() => new(InvokeSequentially);

        public Func<T, CancellationToken, UniTask> ToDelegate() => new(Invoke);

        public override bool Equals(object? obj) => obj is AsyncEventSource<T> source && Equals(source);

        public bool Equals(AsyncEventSource<T> other) => _source == other._source;

        public override int GetHashCode() => _source?.GetHashCode() ?? 0;

        public static bool operator ==(AsyncEventSource<T> left, AsyncEventSource<T> right) => left.Equals(right);

        public static bool operator !=(AsyncEventSource<T> left, AsyncEventSource<T> right) => !(left == right);
    }

    internal sealed class AsyncEventHandlerHolder<T>
    {
        private const int DefaultBufferCapacity = 4;

        // [NOTE]
        // _count == 0    || 1                                   || n (n >= 2)
        // _funcs == null || Func<T, CancellationToken, UniTask> || Func<T, CancellationToken, UniTask>[]
        private object? _funcs;
        private int _count;
        private FastSpinLock _lock;

        public int SubscribedCount => _count;

        internal AsyncEventHandlerHolder()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask InvokeSequentially(T arg, CancellationToken cancellationToken)
        {
            return InvokeCore(arg, cancellationToken, EventInvokeMode.Sequential);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask Invoke(T arg, CancellationToken cancellationToken)
        {
            return InvokeCore(arg, cancellationToken, EventInvokeMode.Parallel);
        }

        private UniTask InvokeCore(T arg, CancellationToken cancellationToken, EventInvokeMode mode)
        {
            if(cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled(cancellationToken);
            }

            // When _count == 0, there is no need to perform exclusive locking.
            if(_count == 0) { return UniTask.CompletedTask; }

            _lock.Enter();          // ---- enter
            // Get _count after exclusive locking.
            var count = _count;
            if(count == 1) {
                var func = SafeCast.NotNullAs<Func<T, CancellationToken, UniTask>>(_funcs);
                _lock.Exit();       // ---- exit
                return func.Invoke(arg, cancellationToken);
            }
            else {
                var funcArray = SafeCast.NotNullAs<Func<T, CancellationToken, UniTask>[]>(_funcs);
                var funcs = funcArray.AsSpan(0, count);
                PooledAsyncEventFuncs<Func<T, CancellationToken, UniTask>> copiedFuncs;
                try {
                    copiedFuncs = new PooledAsyncEventFuncs<Func<T, CancellationToken, UniTask>>(count);
                    funcs.CopyTo(copiedFuncs.AsSpan());
                }
                finally {
                    _lock.Exit();       // ---- exit
                }
                if(mode == EventInvokeMode.Parallel) {
                    return OrderedParallelAsyncEventPromise<T>.CreateTask(copiedFuncs, arg, cancellationToken);
                }
                else {
                    Debug.Assert(mode == EventInvokeMode.Sequential);
                    return OrderedSequentialAsyncEventPromise<T>.CreateTask(copiedFuncs, arg, cancellationToken);
                }
            }
        }

        public Func<T, CancellationToken, UniTask> ToSequentialDelegate()
        {
            return InvokeSequentially;
        }

        public Func<T, CancellationToken, UniTask> ToDelegate()
        {
            return Invoke;
        }

        public void Clear()
        {
            _lock.Enter();          // ---- enter
            _count = 0;
            _funcs = null;
            _lock.Exit();           // ---- exit
        }

        internal void Subscribe(Func<T, CancellationToken, UniTask> func)
        {
            Debug.Assert(func is not null);
            _lock.Enter();          // ---- enter
            try {
                var count = _count;
                if(count == 0) {
                    Debug.Assert(_funcs is null);
                    _funcs = func;
                }
                else if(count == 1) {
                    var funcs = new Func<T, CancellationToken, UniTask>[DefaultBufferCapacity];
                    funcs[0] = SafeCast.NotNullAs<Func<T, CancellationToken, UniTask>>(_funcs);
                    funcs[1] = func;
                    _funcs = funcs;
                }
                else {
                    var funcs = SafeCast.NotNullAs<Func<T, CancellationToken, UniTask>[]>(_funcs);
                    if(funcs.Length == count) {
                        var newFuncs = new Func<T, CancellationToken, UniTask>[funcs.Length * 2];
                        funcs.AsSpan().CopyTo(newFuncs);
                        _funcs = newFuncs;
                        funcs = newFuncs;
                    }
                    funcs[count] = func;
                }
                _count = count + 1;
            }
            finally {
                _lock.Exit();       // ---- exit
            }
            return;
        }

        internal void Unsubscribe(Func<T, CancellationToken, UniTask>? func)
        {
            if(func is null) { return; }
            _lock.Enter();          // ---- enter
            try {
                var funcs = _funcs;
                var count = _count;
                if(count == 0) {
                    Debug.Assert(_funcs == null);
                    return;
                }
                else if(count == 1) {
                    if(ReferenceEquals(_funcs, func)) {
                        _count = 0;
                        _funcs = null;
                    }
                    return;
                }
                else {
                    var funcSpan = SafeCast.NotNullAs<Func<T, CancellationToken, UniTask>[]>(funcs).AsSpan(0, count);
                    for(int i = 0; i < funcSpan.Length; i++) {
                        if(funcSpan[i] == func) {
                            _count = count - 1;
                            if(i < _count) {
                                var copyLen = _count - i;
                                funcSpan.Slice(i + 1, copyLen).CopyTo(funcSpan.Slice(i));
                            }
                            funcSpan[_count] = null!;
                            if(_count == 1) {
                                _funcs = funcSpan[0];
                            }
                            return;
                        }
                    }
                    return;
                }
            }
            finally {
                _lock.Exit();       // ---- exit
            }
        }

        private enum EventInvokeMode : byte
        {
            Parallel = 0,
            Sequential = 1,
        }
    }
}
