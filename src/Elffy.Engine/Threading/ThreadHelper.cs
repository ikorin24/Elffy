#nullable enable
using System;

namespace Elffy.Threading
{
    internal static class ThreadHelper
    {
        [ThreadStatic]
        private static int _threadId;

        public static int CurrentThreadId => (_threadId != 0) ? _threadId : (_threadId = Environment.CurrentManagedThreadId);
    }
}
