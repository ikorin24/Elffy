#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Elffy.Threading
{
    /// <summary>Provides the </summary>
    public static class TaskExtension
    {
        /// <summary>Complete the task synchronously. If it cannot, it will throw an <see cref="InvalidOperationException"/>.</summary>
        /// <param name="task">the task to complete synchronously</param>
        public static void SyncGetResult(this UniTask task)
        {
            var awaiter = task.GetAwaiter();
            if(awaiter.IsCompleted) {
                awaiter.GetResult();
                return;
            }
            ThrowInvalidOperation();
        }

        /// <summary>Complete the task synchronously. If it cannot, it will throw an <see cref="InvalidOperationException"/>.</summary>
        /// <param name="task">the task to complete synchronously</param>
        public static void SyncGetResult(this ValueTask task)
        {
            var awaiter = task.GetAwaiter();
            if(awaiter.IsCompleted) {
                awaiter.GetResult();
                return;
            }
            ThrowInvalidOperation();
        }

        /// <summary>Complete the task synchronously. If it cannot, it will throw an <see cref="InvalidOperationException"/>.</summary>
        /// <param name="task">the task to complete synchronously</param>
        public static void SyncGetResult(this Task task)
        {
            var awaiter = task.GetAwaiter();
            if(awaiter.IsCompleted) {
                awaiter.GetResult();
                return;
            }
            ThrowInvalidOperation();
        }

        /// <summary>Complete the task synchronously. If it cannot, it will throw an <see cref="InvalidOperationException"/>.</summary>
        /// <typeparam name="T">type of the result</typeparam>
        /// <param name="task">the task to complete</param>
        /// <returns>result of the task</returns>
        public static T SyncGetResult<T>(this UniTask<T> task)
        {
            var awaiter = task.GetAwaiter();
            if(awaiter.IsCompleted) {
                return awaiter.GetResult();
            }
            ThrowInvalidOperation();
            return default;
        }

        /// <summary>Complete the task synchronously. If it cannot, it will throw an <see cref="InvalidOperationException"/>.</summary>
        /// <typeparam name="T">type of the result</typeparam>
        /// <param name="task">the task to complete</param>
        /// <returns>result of the task</returns>
        public static T SyncGetResult<T>(this ValueTask<T> task)
        {
            var awaiter = task.GetAwaiter();
            if(awaiter.IsCompleted) {
                return awaiter.GetResult();
            }
            ThrowInvalidOperation();
            return default;
        }

        /// <summary>Complete the task synchronously. If it cannot, it will throw an <see cref="InvalidOperationException"/>.</summary>
        /// <typeparam name="T">type of the result</typeparam>
        /// <param name="task">the task to complete</param>
        /// <returns>result of the task</returns>
        public static T SyncGetResult<T>(this Task<T> task)
        {
            var awaiter = task.GetAwaiter();
            if(awaiter.IsCompleted) {
                return awaiter.GetResult();
            }
            ThrowInvalidOperation();
            return default;
        }

        [DoesNotReturn]
        private static void ThrowInvalidOperation() => throw new InvalidOperationException("Could not complete the task synchronously.");
    }
}
