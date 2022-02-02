#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CA1068 // CancellationToken parameters must come last

namespace Elffy
{
    public sealed class AsyncEventRaiser<T>
    {
        private const int DefaultBufferCapacity = 4;

        // [NOTE]
        // _count == 0    || 1                                   || n (n >= 2)
        // _funcs == null || Func<T, CancellationToken, UniTask> || Func<T, CancellationToken, UniTask>[]
        private object? _funcs;
        private int _count;
        private FastSpinLock _lock;

        public int SubscibedCount => _count;

        public AsyncEventRaiser()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask RaiseSequentially(T arg, CancellationToken cancellationToken = default)
        {
            return RaiseCore(arg, cancellationToken, EventRaiseMode.Sequential);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask Raise(T arg, CancellationToken cancellationToken = default)
        {
            return RaiseCore(arg, cancellationToken, EventRaiseMode.Parallel);
        }

        private UniTask RaiseCore(T arg, CancellationToken cancellationToken, EventRaiseMode mode)
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
                if(mode == EventRaiseMode.Parallel) {
                    return OrderedParallelAsyncEventPromise<T>.CreateTask(copiedFuncs, arg, cancellationToken);
                }
                else {
                    Debug.Assert(mode == EventRaiseMode.Sequential);
                    return OrderedSequentialAsyncEventPromise<T>.CreateTask(copiedFuncs, arg, cancellationToken);
                }
            }
        }

        public Func<T, CancellationToken, UniTask> ToSequentialDelegate()
        {
            return RaiseSequentially;
        }

        public Func<T, CancellationToken, UniTask> ToDelegate()
        {
            return Raise;
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

        private enum EventRaiseMode : byte
        {
            Parallel = 0,
            Sequential = 1,
        }
    }

    public static class EventRaiserExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask RaiseSequentiallyIfNotNull<T>(this AsyncEventRaiser<T>? raiser, T arg, CancellationToken cancellationToken = default)
        {
            if(raiser is not null) {
                return raiser.RaiseSequentially(arg, cancellationToken);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask RaiseIfNotNull<T>(this AsyncEventRaiser<T>? raiser, T arg, CancellationToken cancellationToken = default)
        {
            if(raiser is not null) {
                return raiser.Raise(arg, cancellationToken);
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
    }
}
