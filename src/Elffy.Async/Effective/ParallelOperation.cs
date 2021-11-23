#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace Elffy.Effective
{
    public static class ParallelOperation
    {
        public static UniTask LimitedParallel<TArg>(ReadOnlySpan<Func<TArg, CancellationToken, UniTask>> funcs, TArg arg, int maxParallel, CancellationToken cancellationToken = default)
        {
            if(maxParallel <= 0) {
                throw new ArgumentOutOfRangeException(nameof(maxParallel));
            }
            if(cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled(cancellationToken);
            }
            if(funcs.Length == 0) {
                return UniTask.CompletedTask;
            }
            else if(funcs.Length == 1) {
                return funcs[0].Invoke(arg, cancellationToken);
            }

            if(funcs.Length <= maxParallel) {
                var copiedFuncs = new PooledAsyncEventFuncs<Func<TArg, CancellationToken, UniTask>>(funcs.Length);
                funcs.CopyTo(copiedFuncs.AsSpan());
                return Execute(arg, copiedFuncs, cancellationToken);

                static async UniTask Execute(TArg arg, PooledAsyncEventFuncs<Func<TArg, CancellationToken, UniTask>> copiedFuncs, CancellationToken ct)
                {
                    try {
                        await OrderedParallelAsyncEventPromise<TArg>.CreateTask(copiedFuncs, arg, ct);
                    }
                    finally {
                        copiedFuncs.Return();
                    }
                }
            }
            else {
                // TODO: optimize

                var parallel = new AsyncEventRaiser<TArg>();
                var sequentials = new RefTypeRentMemory<AsyncEventRaiser<TArg>>(maxParallel);
                try {
                    for(int i = 0; i < sequentials.Length; i++) {
                        var r = new AsyncEventRaiser<TArg>();
                        sequentials[i] = r;
                        parallel.Subscribe((arg, ct) => r.RaiseSequentially(arg, ct));
                    }

                    var j = 0;
                    for(int i = 0; i < funcs.Length; i++) {
                        sequentials[j].Subscribe(funcs[i]);
                        j++;
                        if(j == sequentials.Length) {
                            j = 0;
                        }
                    }
                }
                catch {
                    sequentials.Dispose();
                    throw;
                }
                return Execute(parallel, arg, sequentials, cancellationToken);

                static async UniTask Execute(AsyncEventRaiser<TArg> parallel, TArg arg, RefTypeRentMemory<AsyncEventRaiser<TArg>> disposable, CancellationToken ct)
                {
                    try {
                        await parallel.Raise(arg, ct);
                    }
                    finally {
                        disposable.Dispose();
                    }
                }
            }
        }
    }
}
