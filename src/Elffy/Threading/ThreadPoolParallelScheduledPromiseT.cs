#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Threading
{
    internal sealed class ThreadPoolParallelScheduledPromise<TArg> : IUniTaskSource, IThreadPoolParallelScheduledPromise<TArg>
    {
        private ThreadPoolParallelScheduledPromiseCore _core;
        private TArg _arg;

        public ref ThreadPoolParallelScheduledPromiseCore Core => ref _core;

        public TArg Arg => _arg;

        private ThreadPoolParallelScheduledPromise(Action<TArg>[] actions, TArg arg, AsyncBackEndPoint? endPoint, FrameLoopTiming timing)
        {
            _arg = arg;
            _core.Initialize(this, actions, endPoint, timing);
        }

        private ThreadPoolParallelScheduledPromise(Action<TArg> action, int actionCount, TArg arg, AsyncBackEndPoint? endPoint, FrameLoopTiming timing)
        {
            _arg = arg;
            _core.Initialize(this, action, actionCount, endPoint, timing);
        }

        public static UniTask CreateTask(Action<TArg>[] actions, TArg arg)
        {
            if(actions is null) { ThrowNullArg(nameof(actions)); }
            return new UniTask(new ThreadPoolParallelScheduledPromise<TArg>(actions, arg, null, FrameLoopTiming.NotSpecified), 0);
        }

        public static UniTask CreateTask(Action<TArg> action, int actionCount, TArg arg)
        {
            if(action is null) { ThrowNullArg(nameof(action)); }
            return new UniTask(new ThreadPoolParallelScheduledPromise<TArg>(action, actionCount, arg, null, FrameLoopTiming.NotSpecified), 0);
        }

        public static UniTask CreateTaskWithContinueTiming(Action<TArg>[] actions, TArg arg, AsyncBackEndPoint endPoint, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(actions is null) { ThrowNullArg(nameof(actions)); }
            if(endPoint is null) { ThrowNullArg(nameof(endPoint)); }
            return new UniTask(new ThreadPoolParallelScheduledPromise<TArg>(actions, arg, endPoint, timing), 0);
        }

        public static UniTask CreateTaskWithContinueTiming(Action<TArg> action, int actionCount, TArg arg, AsyncBackEndPoint endPoint, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(action is null) { ThrowNullArg(nameof(action)); }
            if(actionCount < 0) { ThrowArgOutOfRange(nameof(actionCount)); }
            return new UniTask(new ThreadPoolParallelScheduledPromise<TArg>(action, actionCount, arg, endPoint, timing), 0);
        }

        public void GetResult(short token) => _core.GetResult(token);

        public UniTaskStatus GetStatus(short token) => _core.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token) => ThreadPoolParallelScheduledPromiseCore.OnCompleted(this, continuation, state, token);

        public UniTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);

        [DoesNotReturn]
        private static void ThrowArgOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);
    }
}
