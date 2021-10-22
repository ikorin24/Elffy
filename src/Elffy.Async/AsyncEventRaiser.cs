#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy
{
    public sealed class AsyncEventRaiser
    {
        private const int DefaultBufferCapacity = 4;

        // [NOTE]
        // _count == 0    || 1                                || n (n >= 2)
        // _funcs == null || Func<CancellationToken, UniTask> || Func<CancellationToken, UniTask>[]
        private object? _funcs;
        private int _count;
        private FastSpinLock _lock;

        public int SubscibedCount => _count;

        public UniTask RaiseSequentially(CancellationToken cancellationToken = default)
        {
            if(cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled(cancellationToken);
            }
            UniTask task;
            _lock.Enter();          // ---- enter
            try {
                var count = _count;
                if(count == 0) {
                    task = UniTask.CompletedTask;
                }
                else if(count == 1) {
                    Debug.Assert(_funcs is not null);
                    var func = Unsafe.As<Func<CancellationToken, UniTask>>(_funcs);
                    task = func.Invoke(cancellationToken);
                }
                else {
                    Debug.Assert(_funcs is Func<CancellationToken, UniTask>[]);
                    var funcs = Unsafe.As<Func<CancellationToken, UniTask>[]>(_funcs).AsSpan(0, count);
                    task = OrderedSequentialAsyncEventPromise.CreateTask(funcs, cancellationToken);
                }
            }
            finally {
                _lock.Exit();       // ---- exit
            }
            return task;
        }

        public Func<CancellationToken, UniTask> ToSequentialDelegate()
        {
            return new Func<CancellationToken, UniTask>(RaiseSequentially);
        }

        public void Clear()
        {
            _lock.Enter();          // ---- enter
            _count = 0;
            _funcs = null;
            _lock.Exit();           // ---- exit
        }

        internal void Subscribe(Func<CancellationToken, UniTask> func)
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
                    Debug.Assert(_funcs is Func<CancellationToken, UniTask>);
                    var funcs = new Func<CancellationToken, UniTask>[DefaultBufferCapacity];
                    funcs[0] = Unsafe.As<Func<CancellationToken, UniTask>>(_funcs);
                    funcs[1] = func;
                    _funcs = funcs;
                }
                else {
                    Debug.Assert(_funcs is not null);
                    Debug.Assert(_funcs is Func<CancellationToken, UniTask>[]);
                    var funcs = Unsafe.As<Func<CancellationToken, UniTask>[]>(_funcs);
                    if(funcs.Length == count) {
                        var newFunc = new Func<CancellationToken, UniTask>[funcs.Length * 2];
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

        internal void Unsubscribe(Func<CancellationToken, UniTask>? func)
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
                    Debug.Assert(funcs is Func<CancellationToken, UniTask>[]);
                    var funcSpan = Unsafe.As<Func<CancellationToken, UniTask>[]>(funcs).AsSpan(0, count);
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
    }

    public static class EventRaiserExtension
    {
        public static UniTask RaiseSequentiallyIfNotNull(this AsyncEventRaiser? raiser, CancellationToken cancellationToken = default)
        {
            if(raiser is not null) {
                return raiser.RaiseSequentially(cancellationToken);
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
