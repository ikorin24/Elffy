#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Imaging.Internal
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowFormatException(string message = "")
        {
            throw new FormatException(message);
        }
    }
}
