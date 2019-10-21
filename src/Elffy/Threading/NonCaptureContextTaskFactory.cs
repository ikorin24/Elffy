using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elffy.Threading
{
    internal static class NonCaptureContextTaskFactory
    {

        public static Task StartNew(Action action)
        {
            var task = Task.Factory.StartNew(action);
            task.ConfigureAwait(false);
            return task;
        }

        public static Task StartNew(Action action, CancellationToken cancellationToken)
        {
            var task = Task.Factory.StartNew(action, cancellationToken);
            task.ConfigureAwait(false);
            return task;
        }

        public static Task<T> StartNew<T>(Func<T> func)
        {
            var task = Task.Factory.StartNew(func);
            task.ConfigureAwait(false);
            return task;
        }

        public static Task<T> StartNew<T>(Func<T> func, CancellationToken cancellationToken)
        {
            var task = Task.Factory.StartNew(func, cancellationToken);
            task.ConfigureAwait(false);
            return task;
        }
    }
}
