﻿#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Imaging.Internal
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowNullArg(string message) => throw new ArgumentNullException(message);

        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowArgException(string message) => throw new ArgumentException(message);

        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowFormatException(string message = "")
        {
            throw new FormatException(message);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowArgOutOfRange(string message)
        {
            throw new ArgumentOutOfRangeException(message);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowInvalidOp(string message)
        {
            throw new InvalidOperationException(message);
        }
    }
}
