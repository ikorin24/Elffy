﻿#nullable enable
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
        public UniTask RaiseParallel(T arg, CancellationToken cancellationToken = default)
        {
            return RaiseCore(arg, cancellationToken, EventRaiseMode.Parallel);
        }

        private UniTask RaiseCore(T arg, CancellationToken cancellationToken, EventRaiseMode mode)
        {
            if(cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled(cancellationToken);
            }
            _lock.Enter();          // ---- enter
            var count = _count;
            if(count == 0) {
                _lock.Exit();       // ---- exit
                return UniTask.CompletedTask;
            }
            else if(count == 1) {
                Debug.Assert(_funcs is not null);
                var func = Unsafe.As<Func<T, CancellationToken, UniTask>>(_funcs);
                _lock.Exit();       // ---- exit
                return func.Invoke(arg, cancellationToken);
            }
            else {
                Debug.Assert(_funcs is Func<T, CancellationToken, UniTask>[]);
                var funcs = Unsafe.As<Func<T, CancellationToken, UniTask>[]>(_funcs).AsSpan(0, count);
                ArraySegment<Func<T, CancellationToken, UniTask>> copiedFuncs;
                try {
                    // TODO: array instance pooling
                    copiedFuncs = funcs.ToArray();
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
            return new Func<T, CancellationToken, UniTask>(RaiseSequentially);
        }

        public Func<T, CancellationToken, UniTask> ToParallelDelegate()
        {
            return new Func<T, CancellationToken, UniTask>(RaiseParallel);
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
                    Debug.Assert(_funcs is Func<T, CancellationToken, UniTask>);
                    var funcs = new Func<T, CancellationToken, UniTask>[DefaultBufferCapacity];
                    funcs[0] = Unsafe.As<Func<T, CancellationToken, UniTask>>(_funcs);
                    funcs[1] = func;
                    _funcs = funcs;
                }
                else {
                    Debug.Assert(_funcs is not null);
                    Debug.Assert(_funcs is Func<T, CancellationToken, UniTask>[]);
                    var funcs = Unsafe.As<Func<T, CancellationToken, UniTask>[]>(_funcs);
                    if(funcs.Length == count) {
                        var newFunc = new Func<T, CancellationToken, UniTask>[funcs.Length * 2];
                        funcs.AsSpan().CopyTo(newFunc);
                        _funcs = newFunc;
                        funcs = newFunc;
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
                if(count == 1) {
                    if(ReferenceEquals(_funcs, func)) {
                        _count = 0;
                        _funcs = null;
                    }
                    _count = 0;
                    _funcs = null;
                    return;
                }
                else {
                    Debug.Assert(funcs is Func<T, CancellationToken, UniTask>[]);
                    var funcSpan = Unsafe.As<Func<T, CancellationToken, UniTask>[]>(funcs).AsSpan(0, count);
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

        public static UniTask RaiseParallelIfNotNull<T>(this AsyncEventRaiser<T>? raiser, T arg, CancellationToken cancellationToken = default)
        {
            if(raiser is not null) {
                return raiser.RaiseParallel(arg, cancellationToken);
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