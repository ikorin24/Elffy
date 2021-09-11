#nullable enable
using Cysharp.Threading.Tasks;
using System;

namespace Elffy.Threading
{
    public static class ParallelOperation
    {
        public static UniTask Parallel(params Action[] actions)
        {
            return ThreadPoolParallelScheduledPromise.CreateTask(actions);
        }

        public static UniTask For(int count, Action action)
        {
            var actions = new Action[count];
            Array.Fill(actions, action);
            return ThreadPoolParallelScheduledPromise.CreateTask(actions);
        }
    }
}
