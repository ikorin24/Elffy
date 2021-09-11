#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Runtime.ExceptionServices;
using Cysharp.Threading.Tasks;
using Elffy.Effective.Unsafes;

namespace Elffy.Threading
{
    internal sealed class ThreadPoolParallelScheduledPromise : IUniTaskSource
    {
        private const int MaxParallelCount = 32;

        private UniTask _whenAllTask;
        private AsyncBackEndPoint? _endPoint;
        private int _nextIndex;
        private bool _queued;
        private UniTaskStatus _status;
        private FrameLoopTiming _timing;
        private ExceptionDispatchInfo? _exceptionInfo;

        private ThreadPoolParallelScheduledPromise(Action?[] actions, AsyncBackEndPoint? endPoint, FrameLoopTiming timing)
        {
            var actionCount = actions.Length;
            _endPoint = endPoint;
            _status = actionCount == 0 ? UniTaskStatus.Succeeded : UniTaskStatus.Pending;
            int parallelCount = Math.Min(actionCount, Math.Min(Environment.ProcessorCount, MaxParallelCount));
            _nextIndex = parallelCount;
            _queued = actionCount > parallelCount;
            _timing = timing;

            // All fields except '_whenAllTask' are set before calling 'CreateWhenAll' method.
            _whenAllTask = CreateWhenAll(this, actions, parallelCount);
        }

        public static UniTask CreateTask(Action[] actions)
        {
            if(actions is null) {
                return UniTask.CompletedTask;
            }
            return new UniTask(new ThreadPoolParallelScheduledPromise(actions, null, FrameLoopTiming.NotSpecified), 0);
        }

        public static UniTask CreateTaskWithContinueTiming(Action[] actions, AsyncBackEndPoint endPoint, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(actions is null) {
                return UniTask.CompletedTask;
            }
            if(endPoint is null) {
                ThrowNull(nameof(endPoint));
            }
            return new UniTask(new ThreadPoolParallelScheduledPromise(actions, endPoint, timing), 0);
        }

#if !DEBUG
        [DebuggerHidden]
#endif
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

#if !DEBUG
        [DebuggerHidden]
#endif
        public async void OnCompleted(Action<object?> continuation, object? state, short token)
        {
            try {
                await _whenAllTask;
            }
            catch(Exception ex) {
                _exceptionInfo = ExceptionDispatchInfo.Capture(ex);
            }
            _status = UniTaskStatus.Succeeded;
            var timing = _timing;
            var endPoint = _endPoint;
            if(endPoint is null || timing.IsSpecified() == false) {
                continuation(state);
            }
            else {
                Debug.Assert(timing.IsSpecified());
                endPoint.Post(timing, continuation, state);
            }
        }

        private static async UniTask OnThreadPool(ThreadPoolParallelScheduledPromise self, Action?[] actions, int index)
        {
            await UniTask.SwitchToThreadPool();
            CheckRangeIfDebug(actions, index);
            actions.At(index)?.Invoke();
            if(self._queued == false) { return; }
            while(true) {
                var nextIndex = Interlocked.Increment(ref self._nextIndex) - 1;
                if(nextIndex >= actions.Length) {
                    return;
                }
                CheckRangeIfDebug(actions, nextIndex);
                actions.At(nextIndex)?.Invoke();
            }
        }

        private static UniTask CreateWhenAll(ThreadPoolParallelScheduledPromise self, Action?[] actions, int parallelCount)
        {
            if(actions.Length == 0) {
                return UniTask.CompletedTask;
            }
            Debug.Assert(parallelCount != 0);
            Debug.Assert(actions.Length >= parallelCount);

            if(parallelCount >= 16) {
                var tasks = new UniTask[parallelCount];
                for(int i = 0; i < tasks.Length; i++) {
                    tasks[i] = OnThreadPool(self, actions, i);
                }
                return UniTask.WhenAll(tasks);
            }

            // Binary search is the fastest.

            if(parallelCount < 8) {
                if(parallelCount < 4) {
                    if(parallelCount < 2) {
                        // 1
                        Debug.Assert(parallelCount == 1);
                        return OnThreadPool(self, actions, 0);
                    }
                    else {
                        if(parallelCount < 3) {
                            // 2
                            Debug.Assert(parallelCount == 2);
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1));
                        }
                        else {
                            // 3
                            Debug.Assert(parallelCount == 3);
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1),
                                                   OnThreadPool(self, actions, 2));
                        }
                    }
                }
                else {
                    if(parallelCount < 6) {
                        if(parallelCount < 5) {
                            // 4
                            Debug.Assert(parallelCount == 4);
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1),
                                                   OnThreadPool(self, actions, 2),
                                                   OnThreadPool(self, actions, 3));
                        }
                        else {
                            // 5
                            Debug.Assert(parallelCount == 5);
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1),
                                                   OnThreadPool(self, actions, 2),
                                                   OnThreadPool(self, actions, 3),
                                                   OnThreadPool(self, actions, 4));
                        }
                    }
                    else {
                        if(parallelCount < 7) {
                            // 6
                            Debug.Assert(parallelCount == 6);
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1),
                                                   OnThreadPool(self, actions, 2),
                                                   OnThreadPool(self, actions, 3),
                                                   OnThreadPool(self, actions, 4),
                                                   OnThreadPool(self, actions, 5));
                        }
                        else {
                            // 7
                            Debug.Assert(parallelCount == 7);
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1),
                                                   OnThreadPool(self, actions, 2),
                                                   OnThreadPool(self, actions, 3),
                                                   OnThreadPool(self, actions, 4),
                                                   OnThreadPool(self, actions, 5),
                                                   OnThreadPool(self, actions, 6));
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
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1),
                                                   OnThreadPool(self, actions, 2),
                                                   OnThreadPool(self, actions, 3),
                                                   OnThreadPool(self, actions, 4),
                                                   OnThreadPool(self, actions, 5),
                                                   OnThreadPool(self, actions, 6),
                                                   OnThreadPool(self, actions, 7));
                        }
                        else {
                            // 9
                            Debug.Assert(parallelCount == 9);
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1),
                                                   OnThreadPool(self, actions, 2),
                                                   OnThreadPool(self, actions, 3),
                                                   OnThreadPool(self, actions, 4),
                                                   OnThreadPool(self, actions, 5),
                                                   OnThreadPool(self, actions, 6),
                                                   OnThreadPool(self, actions, 7),
                                                   OnThreadPool(self, actions, 8));
                        }
                    }
                    else {
                        if(parallelCount < 11) {
                            // 10
                            Debug.Assert(parallelCount == 10);
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1),
                                                   OnThreadPool(self, actions, 2),
                                                   OnThreadPool(self, actions, 3),
                                                   OnThreadPool(self, actions, 4),
                                                   OnThreadPool(self, actions, 5),
                                                   OnThreadPool(self, actions, 6),
                                                   OnThreadPool(self, actions, 7),
                                                   OnThreadPool(self, actions, 8),
                                                   OnThreadPool(self, actions, 9));
                        }
                        else {
                            // 11
                            Debug.Assert(parallelCount == 11);
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1),
                                                   OnThreadPool(self, actions, 2),
                                                   OnThreadPool(self, actions, 3),
                                                   OnThreadPool(self, actions, 4),
                                                   OnThreadPool(self, actions, 5),
                                                   OnThreadPool(self, actions, 6),
                                                   OnThreadPool(self, actions, 7),
                                                   OnThreadPool(self, actions, 8),
                                                   OnThreadPool(self, actions, 9),
                                                   OnThreadPool(self, actions, 10));
                        }
                    }
                }
                else {
                    if(parallelCount < 14) {
                        if(parallelCount < 13) {
                            // 12
                            Debug.Assert(parallelCount == 12);
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1),
                                                   OnThreadPool(self, actions, 2),
                                                   OnThreadPool(self, actions, 3),
                                                   OnThreadPool(self, actions, 4),
                                                   OnThreadPool(self, actions, 5),
                                                   OnThreadPool(self, actions, 6),
                                                   OnThreadPool(self, actions, 7),
                                                   OnThreadPool(self, actions, 8),
                                                   OnThreadPool(self, actions, 9),
                                                   OnThreadPool(self, actions, 10),
                                                   OnThreadPool(self, actions, 11));
                        }
                        else {
                            // 13
                            Debug.Assert(parallelCount == 13);
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1),
                                                   OnThreadPool(self, actions, 2),
                                                   OnThreadPool(self, actions, 3),
                                                   OnThreadPool(self, actions, 4),
                                                   OnThreadPool(self, actions, 5),
                                                   OnThreadPool(self, actions, 6),
                                                   OnThreadPool(self, actions, 7),
                                                   OnThreadPool(self, actions, 8),
                                                   OnThreadPool(self, actions, 9),
                                                   OnThreadPool(self, actions, 10),
                                                   OnThreadPool(self, actions, 11),
                                                   OnThreadPool(self, actions, 12));
                        }
                    }
                    else {
                        if(parallelCount < 15) {
                            // 14
                            Debug.Assert(parallelCount == 14);
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1),
                                                   OnThreadPool(self, actions, 2),
                                                   OnThreadPool(self, actions, 3),
                                                   OnThreadPool(self, actions, 4),
                                                   OnThreadPool(self, actions, 5),
                                                   OnThreadPool(self, actions, 6),
                                                   OnThreadPool(self, actions, 7),
                                                   OnThreadPool(self, actions, 8),
                                                   OnThreadPool(self, actions, 9),
                                                   OnThreadPool(self, actions, 10),
                                                   OnThreadPool(self, actions, 11),
                                                   OnThreadPool(self, actions, 12),
                                                   OnThreadPool(self, actions, 13));
                        }
                        else {
                            // 15
                            Debug.Assert(parallelCount == 15);
                            return UniTask.WhenAll(OnThreadPool(self, actions, 0),
                                                   OnThreadPool(self, actions, 1),
                                                   OnThreadPool(self, actions, 2),
                                                   OnThreadPool(self, actions, 3),
                                                   OnThreadPool(self, actions, 4),
                                                   OnThreadPool(self, actions, 5),
                                                   OnThreadPool(self, actions, 6),
                                                   OnThreadPool(self, actions, 7),
                                                   OnThreadPool(self, actions, 8),
                                                   OnThreadPool(self, actions, 9),
                                                   OnThreadPool(self, actions, 10),
                                                   OnThreadPool(self, actions, 11),
                                                   OnThreadPool(self, actions, 12),
                                                   OnThreadPool(self, actions, 13),
                                                   OnThreadPool(self, actions, 14));
                        }
                    }
                }
            }
        }

        [DoesNotReturn]
        private static void ThrowNull(string message) => throw new ArgumentNullException(message);

        [Conditional("DEBUG")]
        private static void CheckRangeIfDebug<T>(T[] array, int index)
        {
            if((uint)index >= (uint)array.Length) {
                throw new IndexOutOfRangeException();
            }
        }
    }
}
