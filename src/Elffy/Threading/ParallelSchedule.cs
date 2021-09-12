#nullable enable
using Cysharp.Threading.Tasks;
using System;

namespace Elffy.Threading
{
    public static class ParallelSchedule
    {
        public static UniTask Parallel(params Action[] actions)
        {
            return ThreadPoolParallelScheduledPromise.CreateTask(actions);
        }

        public static UniTask Parallel<TArg>(TArg arg, params Action<TArg>[] actions)
        {
            return ThreadPoolParallelScheduledPromise<TArg>.CreateTask(actions, arg);
        }

        public static UniTask For(int count, Action action)
        {
            return ThreadPoolParallelScheduledPromise.CreateTask(action, count);
        }

        public static UniTask For<TArg>(int count, TArg arg, Action<TArg> action)
        {
            return ThreadPoolParallelScheduledPromise<TArg>.CreateTask(action, count, arg);
        }
    }
}
