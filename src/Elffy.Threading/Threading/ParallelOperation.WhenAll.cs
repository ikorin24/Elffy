#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks.Sources;
using System.Diagnostics.CodeAnalysis;
using Elffy.Effective;
using Elffy.Effective.Unsafes;

namespace Elffy.Threading
{
    partial class ParallelOperation
    {
        public delegate void TaskBuildAction<TArg>(Span<UniTask> tasks, in TArg arg);
        public delegate void TaskBuildAction(Span<UniTask> tasks);

        public static UniTask WhenAll<T>(ReadOnlySpan<UniTask<T>> tasks)
        {
            return new UniTask(WhenAllPromise.Create(tasks), 0);
        }

        public static UniTask WhenAll(ReadOnlySpan<UniTask> tasks)
        {
            return new UniTask(WhenAllPromise.Create(tasks), 0);
        }

        public static UniTask WhenAll(int taskCount, TaskBuildAction taskBuilder)
        {
            if(taskBuilder is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(taskBuilder));
            }
            var rent = UniTaskMemoryPool.Rent(taskCount);
            try {
                var tasks = rent.AsSpan(0, taskCount);
                taskBuilder.Invoke(tasks);
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll<TState>(int taskCount, in TState state, TaskBuildAction<TState> taskBuilder)
        {
            if(taskBuilder is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(taskBuilder));
            }
            var rent = UniTaskMemoryPool.Rent(taskCount);
            try {
                var tasks = rent.AsSpan(0, taskCount);
                taskBuilder.Invoke(tasks, in state);
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2)
        {
            const int TaskCount = 2;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3)
        {
            const int TaskCount = 3;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3, UniTask task4)
        {
            const int TaskCount = 4;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                tasks.At(3) = task4;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3, UniTask task4,
                                      UniTask task5)
        {
            const int TaskCount = 5;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                tasks.At(3) = task4;
                tasks.At(4) = task5;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3, UniTask task4,
                                      UniTask task5, UniTask task6)
        {
            const int TaskCount = 6;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                tasks.At(3) = task4;
                tasks.At(4) = task5;
                tasks.At(5) = task6;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3, UniTask task4,
                                      UniTask task5, UniTask task6, UniTask task7)
        {
            const int TaskCount = 7;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                tasks.At(3) = task4;
                tasks.At(4) = task5;
                tasks.At(5) = task6;
                tasks.At(6) = task7;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3, UniTask task4,
                                      UniTask task5, UniTask task6, UniTask task7, UniTask task8)
        {
            const int TaskCount = 8;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                tasks.At(3) = task4;
                tasks.At(4) = task5;
                tasks.At(5) = task6;
                tasks.At(6) = task7;
                tasks.At(7) = task8;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3, UniTask task4,
                                      UniTask task5, UniTask task6, UniTask task7, UniTask task8,
                                      UniTask task9)
        {
            const int TaskCount = 9;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                tasks.At(3) = task4;
                tasks.At(4) = task5;
                tasks.At(5) = task6;
                tasks.At(6) = task7;
                tasks.At(7) = task8;
                tasks.At(8) = task9;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3, UniTask task4,
                                      UniTask task5, UniTask task6, UniTask task7, UniTask task8,
                                      UniTask task9, UniTask task10)
        {
            const int TaskCount = 10;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                tasks.At(3) = task4;
                tasks.At(4) = task5;
                tasks.At(5) = task6;
                tasks.At(6) = task7;
                tasks.At(7) = task8;
                tasks.At(8) = task9;
                tasks.At(9) = task10;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3, UniTask task4,
                                      UniTask task5, UniTask task6, UniTask task7, UniTask task8,
                                      UniTask task9, UniTask task10, UniTask task11)
        {
            const int TaskCount = 11;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                tasks.At(3) = task4;
                tasks.At(4) = task5;
                tasks.At(5) = task6;
                tasks.At(6) = task7;
                tasks.At(7) = task8;
                tasks.At(8) = task9;
                tasks.At(9) = task10;
                tasks.At(10) = task11;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3, UniTask task4,
                                      UniTask task5, UniTask task6, UniTask task7, UniTask task8,
                                      UniTask task9, UniTask task10, UniTask task11, UniTask task12)
        {
            const int TaskCount = 12;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                tasks.At(3) = task4;
                tasks.At(4) = task5;
                tasks.At(5) = task6;
                tasks.At(6) = task7;
                tasks.At(7) = task8;
                tasks.At(8) = task9;
                tasks.At(9) = task10;
                tasks.At(10) = task11;
                tasks.At(11) = task12;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3, UniTask task4,
                                      UniTask task5, UniTask task6, UniTask task7, UniTask task8,
                                      UniTask task9, UniTask task10, UniTask task11, UniTask task12,
                                      UniTask task13)
        {
            const int TaskCount = 13;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                tasks.At(3) = task4;
                tasks.At(4) = task5;
                tasks.At(5) = task6;
                tasks.At(6) = task7;
                tasks.At(7) = task8;
                tasks.At(8) = task9;
                tasks.At(9) = task10;
                tasks.At(10) = task11;
                tasks.At(11) = task12;
                tasks.At(12) = task13;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3, UniTask task4,
                                      UniTask task5, UniTask task6, UniTask task7, UniTask task8,
                                      UniTask task9, UniTask task10, UniTask task11, UniTask task12,
                                      UniTask task13, UniTask task14)
        {
            const int TaskCount = 14;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                tasks.At(3) = task4;
                tasks.At(4) = task5;
                tasks.At(5) = task6;
                tasks.At(6) = task7;
                tasks.At(7) = task8;
                tasks.At(8) = task9;
                tasks.At(9) = task10;
                tasks.At(10) = task11;
                tasks.At(11) = task12;
                tasks.At(12) = task13;
                tasks.At(13) = task14;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3, UniTask task4,
                                      UniTask task5, UniTask task6, UniTask task7, UniTask task8,
                                      UniTask task9, UniTask task10, UniTask task11, UniTask task12,
                                      UniTask task13, UniTask task14, UniTask task15)
        {
            const int TaskCount = 15;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                tasks.At(3) = task4;
                tasks.At(4) = task5;
                tasks.At(5) = task6;
                tasks.At(6) = task7;
                tasks.At(7) = task8;
                tasks.At(8) = task9;
                tasks.At(9) = task10;
                tasks.At(10) = task11;
                tasks.At(11) = task12;
                tasks.At(12) = task13;
                tasks.At(13) = task14;
                tasks.At(14) = task15;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }

        public static UniTask WhenAll(UniTask task1, UniTask task2, UniTask task3, UniTask task4,
                                      UniTask task5, UniTask task6, UniTask task7, UniTask task8,
                                      UniTask task9, UniTask task10, UniTask task11, UniTask task12,
                                      UniTask task13, UniTask task14, UniTask task15, UniTask task16)
        {
            const int TaskCount = 16;
            var rent = UniTaskMemoryPool.Rent(TaskCount);
            try {
                var tasks = rent.AsSpan(0, TaskCount);
                Debug.Assert(tasks.Length == TaskCount);
                tasks.At(0) = task1;
                tasks.At(1) = task2;
                tasks.At(2) = task3;
                tasks.At(3) = task4;
                tasks.At(4) = task5;
                tasks.At(5) = task6;
                tasks.At(6) = task7;
                tasks.At(7) = task8;
                tasks.At(8) = task9;
                tasks.At(9) = task10;
                tasks.At(10) = task11;
                tasks.At(11) = task12;
                tasks.At(12) = task13;
                tasks.At(13) = task14;
                tasks.At(14) = task15;
                tasks.At(15) = task16;
                return WhenAll(tasks);
            }
            finally {
                UniTaskMemoryPool.Return(rent);
            }
        }


        private sealed class PromiseAwaiterPair<TAwaiter> : IChainInstancePooled<PromiseAwaiterPair<TAwaiter>> where TAwaiter : struct
        {
            private IUniTaskSource? _promise;
            private TAwaiter _awaiter;
            private PromiseAwaiterPair<TAwaiter>? _next;

            public ref PromiseAwaiterPair<TAwaiter>? NextPooled => ref _next;

            private PromiseAwaiterPair(IUniTaskSource promise, in TAwaiter awaiter)
            {
                _promise = promise;
                _awaiter = awaiter;
            }

            public static PromiseAwaiterPair<TAwaiter> Create<TPromise>(TPromise promise, in TAwaiter awaiter) where TPromise : class, IUniTaskSource
            {
                if(ChainInstancePool<PromiseAwaiterPair<TAwaiter>>.TryGetInstanceFast(out var instance)) {
                    instance._promise = promise;
                    instance._awaiter = awaiter;
                }
                else {
                    instance = new PromiseAwaiterPair<TAwaiter>(promise, awaiter);
                }
                return instance;
            }

            public static (TPromise Promise, TAwaiter Awaiter) Extract<TPromise>(object obj) where TPromise : class, IUniTaskSource
            {
                Debug.Assert(obj is PromiseAwaiterPair<TAwaiter>);
                var s = Unsafe.As<PromiseAwaiterPair<TAwaiter>>(obj);
                Debug.Assert(s._promise is not null);
                Debug.Assert(s._promise is TPromise);
                var promise = Unsafe.As<TPromise>(s._promise);
                var awaiter = s._awaiter;
                s._awaiter = default;
                s._promise = default;
                ChainInstancePool<PromiseAwaiterPair<TAwaiter>>.ReturnInstanceFast(s);
                return (promise, awaiter);
            }
        }

        private sealed class WhenAllPromise : IUniTaskSource, IValueTaskSource
        {
            private int _completeCount;
            private int _tasksLength;
            private UniTaskCompletionSourceCore<AsyncUnit> _core;

            private WhenAllPromise()
            {
            }

            public static WhenAllPromise Create(ReadOnlySpan<UniTask> tasks)
            {
                var promise = new WhenAllPromise();
                promise.Init(tasks);
                return promise;
            }

            public static WhenAllPromise Create<T>(ReadOnlySpan<UniTask<T>> tasks)
            {
                var promise = new WhenAllPromise();
                promise.Init(tasks);
                return promise;
            }

            private void Init(ReadOnlySpan<UniTask> tasks)
            {
                _tasksLength = tasks.Length;
                _completeCount = 0;
                if(tasks.Length == 0) {
                    _core.TrySetResult(AsyncUnit.Default);
                    return;
                }
                for(int i = 0; i < tasks.Length; i++) {
                    InitEachTask(tasks[i]);
                }
            }

            private void Init<T>(ReadOnlySpan<UniTask<T>> tasks)
            {
                _tasksLength = tasks.Length;
                _completeCount = 0;
                if(tasks.Length == 0) {
                    _core.TrySetResult(AsyncUnit.Default);
                    return;
                }
                for(int i = 0; i < tasks.Length; i++) {
                    InitEachTask(tasks[i].AsUniTask());
                }
            }

            private void InitEachTask(in UniTask task)
            {
                UniTask.Awaiter awaiter;
                try {
                    awaiter = task.GetAwaiter();
                }
                catch(Exception error) {
                    _core.TrySetException(error);
                    return;
                }

                if(awaiter.IsCompleted) {
                    TryInvokeContinuation(this, in awaiter);
                    return;
                }

                awaiter.SourceOnCompleted(static state =>
                {
                    var (self, awaiter) = PromiseAwaiterPair<UniTask.Awaiter>.Extract<WhenAllPromise>(state);
                    TryInvokeContinuation(self, in awaiter);
                }, PromiseAwaiterPair<UniTask.Awaiter>.Create(this, awaiter));
            }

            private static void TryInvokeContinuation(WhenAllPromise self, in UniTask.Awaiter awaiter)
            {
                try {
                    awaiter.GetResult();
                }
                catch(Exception ex) {
                    self._core.TrySetException(ex);
                    return;
                }

                if(Interlocked.Increment(ref self._completeCount) == self._tasksLength) {
                    self._core.TrySetResult(AsyncUnit.Default);
                }
            }

            public void GetResult(short token)
            {
                //#pragma warning disable CA1816
                //                GC.SuppressFinalize(this);
                //#pragma warning restore CA1816
                _core.GetResult(token);
            }

            public UniTaskStatus GetStatus(short token) => _core.GetStatus(token);

            public UniTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

            public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);
        }
    }
}
