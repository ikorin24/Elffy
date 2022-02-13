#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public sealed class ContextMismatchException : Exception
    {
        private const string DefaultMessage = "Current context is invalid.";

        private ContextMismatchException(string message) : base(message)
        {
        }

        public ContextMismatchException(IHostScreen? currentContext, IHostScreen? expectedContext)
            : base(CreateExceptionMessage(currentContext, expectedContext))
        {
        }

        private static string CreateExceptionMessage(IHostScreen? currentContext, IHostScreen? expectedContext)
        {
            var current = currentContext?.Title ?? "null";
            var expected = expectedContext?.Title ?? "null";
            return $"{DefaultMessage} (Current Context: {current}, Expected Context: {expected})";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfContextNotEqual(IHostScreen actual, IHostScreen expected)
        {
            Debug.Assert(actual != null);
            Debug.Assert(expected != null);
            if(actual != expected) {
                Throw(actual, expected);
            }
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void Throw(IHostScreen? currentContext, IHostScreen? expectedContext)
        {
            throw new ContextMismatchException(currentContext, expectedContext);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowCurrentContextIsNull()
        {
            throw new ContextMismatchException($"Current context must not be null. You may be in the wrong thread or timing.");
        }
    }
}
