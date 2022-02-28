#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowInvalidNullScreen()
        {
            throw new InvalidOperationException("The object does not have owner screen.");
        }
    }
}
