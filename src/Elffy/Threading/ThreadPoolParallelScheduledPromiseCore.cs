#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.ExceptionServices;
using Cysharp.Threading.Tasks;
using Elffy.Effective.Unsafes;

namespace Elffy.Threading
{
    internal struct ThreadPoolParallelScheduledPromiseCore
    {
        private const int MaxParallelCount = 32;

        private UniTask _whenAllTask;
        private AsyncBackEndPoint? _endPoint;
        private int _parallelCount;
        private int _nextIndex;
        private UniTaskStatus _status;
        private FrameLoopTiming _timing;
        private ExceptionDispatchInfo? _exceptionInfo;

        public void Initialize<TPromise>(TPromise promise, Action?[] actions, AsyncBackEndPoint? endPoint, FrameLoopTiming timing)
            where TPromise : class, IThreadPoolParallelScheduledPromise
        {
            InitializePrivate<TPromise, DummyArg>(promise, actions, endPoint, timing);
        }

        public void Initialize<TPromise, TArg>(TPromise promise, Action<TArg>?[] actions, AsyncBackEndPoint? endPoint, FrameLoopTiming timing)
            where TPromise : class, IThreadPoolParallelScheduledPromise
        {
            InitializePrivate<TPromise, TArg>(promise, actions, endPoint, timing);
        }

        private void InitializePrivate<TPromise, TArg>(TPromise promise, Delegate?[] actions, AsyncBackEndPoint? endPoint, FrameLoopTiming timing)
            where TPromise : class, IThreadPoolParallelScheduledPromise
        {
            var actionCount = actions.Length;
            _endPoint = endPoint;
            _status = actionCount == 0 ? UniTaskStatus.Succeeded : UniTaskStatus.Pending;
            _parallelCount = Math.Min(actionCount, Math.Min(Environment.ProcessorCount, MaxParallelCount));
            _nextIndex = _parallelCount;
            _timing = timing;
            _exceptionInfo = null;

            // All fields except '_whenAllTask' are set before calling 'CreateWhenAll' method.
            _whenAllTask = CreateWhenAll<TPromise, TArg>(promise, actions);
        }

        public void Initialize<TPromise>(TPromise promise, Action action, int actionCount, AsyncBackEndPoint? endPoint, FrameLoopTiming timing)
            where TPromise : class, IThreadPoolParallelScheduledPromise
        {
            InitializePrivate<TPromise, DummyArg>(promise, action, actionCount, endPoint, timing);
        }

        public void Initialize<TPromise, TArg>(TPromise promise, Action<TArg> action, int actionCount, AsyncBackEndPoint? endPoint, FrameLoopTiming timing)
            where TPromise : class, IThreadPoolParallelScheduledPromise
        {
            InitializePrivate<TPromise, TArg>(promise, action, actionCount, endPoint, timing);
        }

        private void InitializePrivate<TPromise, TArg>(TPromise promise, Delegate action, int actionCount, AsyncBackEndPoint? endPoint, FrameLoopTiming timing)
            where TPromise : class, IThreadPoolParallelScheduledPromise
        {
            Debug.Assert(actionCount >= 0);

            _endPoint = endPoint;
            _status = actionCount == 0 ? UniTaskStatus.Succeeded : UniTaskStatus.Pending;
            _parallelCount = Math.Min(actionCount, Math.Min(Environment.ProcessorCount, MaxParallelCount));
            _nextIndex = _parallelCount;
            _timing = timing;
            _exceptionInfo = null;

            // All fields except '_whenAllTask' are set before calling 'CreateWhenAll' method.
            _whenAllTask = CreateWhenAll<TPromise, TArg>(promise, action, actionCount);
        }

        public void GetResult(short token)
        {
            _exceptionInfo?.Throw();
            if(_status != UniTaskStatus.Succeeded) {
                throw new InvalidOperationException();
            }
            return;
        }

        public UniTaskStatus GetStatus(short token) => UnsafeGetStatus();

        public UniTaskStatus UnsafeGetStatus()
        {
            if(_exceptionInfo is not null) {
                return UniTaskStatus.Faulted;
            }
            return _status;
        }

        public static async void OnCompleted<TPromise>(TPromise promise, Action<object?> continuation, object? state, short token)
            where TPromise : class, IThreadPoolParallelScheduledPromise
        {
            try {
                await promise.Core._whenAllTask;
            }
            catch(Exception ex) {
                promise.Core._exceptionInfo = ExceptionDispatchInfo.Capture(ex);
            }
            promise.Core._status = UniTaskStatus.Succeeded;
            var timing = promise.Core._timing;
            var endPoint = promise.Core._endPoint;
            if(endPoint is null || timing.IsSpecified() == false) {
                continuation(state);
            }
            else {
                Debug.Assert(timing.IsSpecified());
                endPoint.Post(timing, continuation, state);
            }
        }

        private static UniTask OnThreadPool<TPromise, TArg>(TPromise promise, Delegate action, int actionCount)
            where TPromise : class, IThreadPoolParallelScheduledPromise
        {
            if(typeof(TArg) == typeof(DummyArg)) {
                return OnThreadPoolNoArg(promise, SafeCast.As<Action>(action), actionCount);
            }
            else {
                return OnThreadPoolWithArg(SafeCast.As<IThreadPoolParallelScheduledPromise<TArg>>(promise),
                                           SafeCast.As<Action<TArg>>(action),
                                           actionCount);
            }

            static async UniTask OnThreadPoolNoArg(IThreadPoolParallelScheduledPromise promise, Action action, int actionCount)
            {
                await UniTask.SwitchToThreadPool();
                action.Invoke();
                while(true) {
                    var nextIndex = Interlocked.Increment(ref promise.Core._nextIndex) - 1;
                    if(nextIndex >= actionCount) {
                        return;
                    }
                    action.Invoke();
                }
            }

            static async UniTask OnThreadPoolWithArg(IThreadPoolParallelScheduledPromise<TArg> promise, Action<TArg> action, int actionCount)
            {
                await UniTask.SwitchToThreadPool();
                action.Invoke(promise.Arg);
                while(true) {
                    var nextIndex = Interlocked.Increment(ref promise.Core._nextIndex) - 1;
                    if(nextIndex >= actionCount) {
                        return;
                    }
                    action.Invoke(promise.Arg);
                }
            }
        }


        private static UniTask OnThreadPool<TPromise, TArg>(TPromise promise, Delegate?[] actions, int index)
            where TPromise : class, IThreadPoolParallelScheduledPromise
        {
            if(typeof(TArg) == typeof(DummyArg)) {
                return OnThreadPoolNoArg(promise, SafeCast.As<Action[]>(actions), index);
            }
            else {
                return OnThreadPoolWithArg(SafeCast.As<IThreadPoolParallelScheduledPromise<TArg>>(promise),
                                           SafeCast.As<Action<TArg>[]>(actions),
                                           index);
            }

            static async UniTask OnThreadPoolNoArg(IThreadPoolParallelScheduledPromise promise, Action?[] actions, int index)
            {
                await UniTask.SwitchToThreadPool();
                CheckRangeIfDebug(actions, index);
                actions.At(index)?.Invoke();
                while(true) {
                    var nextIndex = Interlocked.Increment(ref promise.Core._nextIndex) - 1;
                    if(nextIndex >= actions.Length) {
                        return;
                    }
                    CheckRangeIfDebug(actions, nextIndex);
                    actions.At(nextIndex)?.Invoke();
                }
            }

            static async UniTask OnThreadPoolWithArg(IThreadPoolParallelScheduledPromise<TArg> promise, Action<TArg>?[] actions, int index)
            {
                await UniTask.SwitchToThreadPool();
                CheckRangeIfDebug(actions, index);
                actions.At(index)?.Invoke(promise.Arg);
                while(true) {
                    var nextIndex = Interlocked.Increment(ref promise.Core._nextIndex) - 1;
                    if(nextIndex >= actions.Length) {
                        return;
                    }
                    CheckRangeIfDebug(actions, nextIndex);
                    actions.At(nextIndex)?.Invoke(promise.Arg);
                }
            }
        }

        private static UniTask CreateWhenAll<TPromise, TArg>(TPromise promise, Delegate action, int actionCount)
            where TPromise : class, IThreadPoolParallelScheduledPromise
        {
            if(actionCount == 0) {
                return UniTask.CompletedTask;
            }
            var parallelCount = promise.Core._parallelCount;
            Debug.Assert(parallelCount != 0);
            Debug.Assert(actionCount >= parallelCount);

            if(parallelCount >= 16) {
                var tasks = new UniTask[parallelCount];
                for(int i = 0; i < tasks.Length; i++) {
                    tasks[i] = OnThreadPool<TPromise, TArg>(promise, action, actionCount);
                }
                return UniTask.WhenAll(tasks);
            }

            // TODO:
            {
                var tasks = new UniTask[parallelCount];
                for(int i = 0; i < tasks.Length; i++) {
                    tasks[i] = OnThreadPool<TPromise, TArg>(promise, action, actionCount);
                }
                return UniTask.WhenAll(tasks);
            }
        }

        private static UniTask CreateWhenAll<TPromise, TArg>(TPromise promise, Delegate?[] actions)
            where TPromise : class, IThreadPoolParallelScheduledPromise
        {
            if(actions.Length == 0) {
                return UniTask.CompletedTask;
            }
            var parallelCount = promise.Core._parallelCount;
            Debug.Assert(parallelCount != 0);
            Debug.Assert(actions.Length >= parallelCount);

            if(parallelCount >= 16) {
                var tasks = new UniTask[parallelCount];
                for(int i = 0; i < tasks.Length; i++) {
                    tasks[i] = OnThreadPool<TPromise, TArg>(promise, actions, i);
                }
                return UniTask.WhenAll(tasks);
            }

            // Binary search is the fastest.

            if(parallelCount < 8) {
                if(parallelCount < 4) {
                    if(parallelCount < 2) {
                        // 1
                        Debug.Assert(parallelCount == 1);
                        return OnThreadPool<TPromise, TArg>(promise, actions, 0);
                    }
                    else {
                        if(parallelCount < 3) {
                            // 2
                            Debug.Assert(parallelCount == 2);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1));
                        }
                        else {
                            // 3
                            Debug.Assert(parallelCount == 3);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 2));
                        }
                    }
                }
                else {
                    if(parallelCount < 6) {
                        if(parallelCount < 5) {
                            // 4
                            Debug.Assert(parallelCount == 4);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 2),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 3));
                        }
                        else {
                            // 5
                            Debug.Assert(parallelCount == 5);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 2),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 3),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 4));
                        }
                    }
                    else {
                        if(parallelCount < 7) {
                            // 6
                            Debug.Assert(parallelCount == 6);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 2),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 3),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 4),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 5));
                        }
                        else {
                            // 7
                            Debug.Assert(parallelCount == 7);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 2),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 3),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 4),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 5),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 6));
                        }
                    }
                }
            }
            else {
                if(parallelCount < 12) {
                    if(parallelCount < 10) {
                        if(parallelCount < 9) {
                            // 8
                            Debug.Assert(parallelCount == 8);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 2),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 3),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 4),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 5),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 6),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 7));
                        }
                        else {
                            // 9
                            Debug.Assert(parallelCount == 9);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 2),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 3),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 4),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 5),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 6),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 7),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 8));
                        }
                    }
                    else {
                        if(parallelCount < 11) {
                            // 10
                            Debug.Assert(parallelCount == 10);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 2),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 3),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 4),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 5),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 6),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 7),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 8),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 9));
                        }
                        else {
                            // 11
                            Debug.Assert(parallelCount == 11);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 2),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 3),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 4),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 5),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 6),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 7),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 8),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 9),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 10));
                        }
                    }
                }
                else {
                    if(parallelCount < 14) {
                        if(parallelCount < 13) {
                            // 12
                            Debug.Assert(parallelCount == 12);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 2),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 3),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 4),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 5),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 6),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 7),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 8),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 9),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 10),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 11));
                        }
                        else {
                            // 13
                            Debug.Assert(parallelCount == 13);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 2),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 3),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 4),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 5),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 6),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 7),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 8),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 9),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 10),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 11),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 12));
                        }
                    }
                    else {
                        if(parallelCount < 15) {
                            // 14
                            Debug.Assert(parallelCount == 14);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 2),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 3),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 4),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 5),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 6),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 7),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 8),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 9),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 10),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 11),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 12),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 13));
                        }
                        else {
                            // 15
                            Debug.Assert(parallelCount == 15);
                            return UniTask.WhenAll(OnThreadPool<TPromise, TArg>(promise, actions, 0),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 1),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 2),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 3),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 4),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 5),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 6),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 7),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 8),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 9),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 10),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 11),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 12),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 13),
                                                   OnThreadPool<TPromise, TArg>(promise, actions, 14));
                        }
                    }
                }
            }
        }

        [Conditional("DEBUG")]
        private static void CheckRangeIfDebug<T>(T[] array, int index)
        {
            if((uint)index >= (uint)array.Length) {
                throw new IndexOutOfRangeException();
            }
        }

        internal sealed class DummyArg
        {
            public static DummyArg? Default => null;
        }
    }

    internal interface IThreadPoolParallelScheduledPromise
    {
        ref ThreadPoolParallelScheduledPromiseCore Core { get; }
    }

    internal interface IThreadPoolParallelScheduledPromise<TArg> : IThreadPoolParallelScheduledPromise
    {
        TArg Arg { get; }
    }
}
