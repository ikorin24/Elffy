#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;

namespace Elffy.Threading
{
    internal sealed class ThreadPoolParallelScheduledPromise : IUniTaskSource, IThreadPoolParallelScheduledPromise
    {
        private ThreadPoolParallelScheduledPromiseCore _core;

        public ref ThreadPoolParallelScheduledPromiseCore Core => ref _core;

        private ThreadPoolParallelScheduledPromise(Action?[] actions, AsyncBackEndPoint? endPoint, FrameLoopTiming timing)
        {
            _core.Initialize(this, actions, endPoint, timing);
        }

        private ThreadPoolParallelScheduledPromise(Action action, int actionCount, AsyncBackEndPoint? endPoint, FrameLoopTiming timing)
        {
            _core.Initialize(this, action, actionCount, endPoint, timing);
        }

        public static UniTask CreateTask(Action[] actions)
        {
            if(actions is null) { ThrowNullArg(nameof(actions)); }
            return new UniTask(new ThreadPoolParallelScheduledPromise(actions, null, FrameLoopTiming.NotSpecified), 0);
        }

        public static UniTask CreateTask(Action action, int actionCount)
        {
            if(action is null) { ThrowNullArg(nameof(action)); }
            return new UniTask(new ThreadPoolParallelScheduledPromise(action, actionCount, null, FrameLoopTiming.NotSpecified), 0);
        }

        public static UniTask CreateTaskWithContinueTiming(Action[] actions, AsyncBackEndPoint endPoint, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(actions is null) { ThrowNullArg(nameof(actions)); }
            if(endPoint is null) { ThrowNullArg(nameof(endPoint)); }
            return new UniTask(new ThreadPoolParallelScheduledPromise(actions, endPoint, timing), 0);
        }

        public static UniTask CreateTaskWithContinueTiming(Action action, int actionCount, AsyncBackEndPoint endPoint, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(action is null) { ThrowNullArg(nameof(action)); }
            if(actionCount < 0) { ThrowArgOutOfRange(nameof(actionCount)); }
            return new UniTask(new ThreadPoolParallelScheduledPromise(action, actionCount, endPoint, timing), 0);
        }

        public void GetResult(short token) => _core.GetResult(token);

        public UniTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public UniTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        public void OnCompleted(Action<object?> continuation, object? state, short token) => ThreadPoolParallelScheduledPromiseCore.OnCompleted(this, continuation, state, token);

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);

        [DoesNotReturn]
        private static void ThrowArgOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);
    }
}
