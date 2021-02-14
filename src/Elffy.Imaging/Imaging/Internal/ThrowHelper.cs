#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Imaging.Internal
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowNullArg(string message) => throw new ArgumentNullException(message);

        [DoesNotReturn]
        public static void ThrowFormatException(string message = "")
        {
            throw new FormatException(message);
        }

        [DoesNotReturn]
        public static void ThrowArgOutOfRange(string message)
        {
            throw new ArgumentOutOfRangeException(message);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOp(string message)
        {
            throw new InvalidOperationException(message);
        }
    }
}
