﻿#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy.Threading
{
    public sealed partial class ParallelOperation
    {
        private const int MinCapacity = 16;

        private UniTaskRentArray _array;
        private int _count;
        private FastSpinLock _lock;
        private bool _isDead;

        public ParallelOperation()
        {
        }

        public void Add(UniTask task)
        {
            _lock.Enter();
            try {
                if(task.Status == UniTaskStatus.Succeeded) { return; }
                if(_isDead) {
                    Throw();
                    [DoesNotReturn] static void Throw() => throw new InvalidOperationException($"Can not add a task after call '{nameof(WhenAll)}' method.");
                }
                if(_array.Length == _count) {
                    ResizeBuffer(ref _array);
                }
                Debug.Assert(_array.Length > _count);
                _array[_count] = task;
                _count++;
            }
            finally {
                _lock.Exit();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void ResizeBuffer(ref UniTaskRentArray currentArray)
            {
                var newArray = UniTaskMemoryPool.Rent(Math.Max(MinCapacity, currentArray.Length * 2));
                currentArray.AsSpan().CopyTo(newArray.AsSpan());
                UniTaskMemoryPool.Return(currentArray);
                currentArray = newArray;
            }
        }

        public UniTask WhenAll()
        {
            _lock.Enter();
            try {
                if(_isDead) {
                    ThrowCannotCallTwice();
                    [DoesNotReturn] static void ThrowCannotCallTwice() => throw new InvalidOperationException($"Can not call {nameof(WhenAll)} method twice.");
                }
                _isDead = true;
                return WhenAll(_array.AsSpan(0, _count));
            }
            finally {
                UniTaskMemoryPool.Return(_array);
                _array = UniTaskRentArray.Empty;
                _lock.Exit();
            }
        }

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
                return OrderedParallelAsyncEventPromise<TArg>.CreateTask(copiedFuncs, arg, cancellationToken);
            }
            else {
                var runner = new LimitedParallelRunner<TArg>(funcs, arg, maxParallel);
                return Execute(runner, cancellationToken);

                static async UniTask Execute(LimitedParallelRunner<TArg> runner, CancellationToken ct)
                {
                    try {
                        await runner.Execute(ct);
                    }
                    finally {
                        runner.Dispose();
                    }
                }
            }
        }

        // [NOTE] Don't change this class into struct.
        private sealed class LimitedParallelRunner<TArg> : IDisposable
        {
            private RefTypeRentMemory<Func<TArg, CancellationToken, UniTask>> _funcs;
            private TArg _arg;
            private readonly int _maxParallel;
            private int _channelId;

            public LimitedParallelRunner(ReadOnlySpan<Func<TArg, CancellationToken, UniTask>> funcs, in TArg arg, int maxParallel)
            {
                _arg = arg;
                _maxParallel = maxParallel;
                _channelId = 0;
                _funcs = new RefTypeRentMemory<Func<TArg, CancellationToken, UniTask>>(funcs.Length);
                funcs.CopyTo(_funcs.AsSpan());
            }

            public UniTask Execute(CancellationToken ct)
            {
                var parallelFuncs = new PooledAsyncEventFuncs<Func<LimitedParallelRunner<TArg>, CancellationToken, UniTask>>(_maxParallel);
                var span = parallelFuncs.AsSpan();
                for(int i = 0; i < parallelFuncs.Count; i++) {
                    span[i] = static (self, ct) => self.ExecuteChannel(ct);
                }
                return OrderedParallelAsyncEventPromise<LimitedParallelRunner<TArg>>.CreateTask(parallelFuncs, this, ct);
            }

            public void Dispose()
            {
                _funcs.Dispose();
            }

            private UniTask ExecuteChannel(CancellationToken ct)
            {
                var maxParallel = _maxParallel;
                var parallelId = Interlocked.Increment(ref _channelId) - 1;
                var funcs = _funcs.AsSpan();
                var q = Math.DivRem(funcs.Length, maxParallel, out var r);
                var funcCount = q + ((parallelId < r) ? 1 : 0);

                var sequentialFuncs = new PooledAsyncEventFuncs<Func<TArg, CancellationToken, UniTask>>(funcCount);
                var sf = sequentialFuncs.AsSpan();
                for(int i = 0; i < sf.Length; i++) {
                    sf[i] = funcs[i * maxParallel];
                }
                return OrderedSequentialAsyncEventPromise<TArg>.CreateTask(sequentialFuncs, _arg, ct);
            }
        }
    }
}