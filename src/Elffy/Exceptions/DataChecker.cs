#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Elffy.Exceptions
{
    /// <summary>Data checker class. Switch enable/disable checking by conditional compiler.</summary>
    internal static class DataChecker
    {
        /// <summary>Symbol of conditional compiling</summary>
        private const string CHECK_DATA = "CHECK_DATA";

        /// <summary>Throw <see cref="InvalidDataException"/> if <paramref name="condition"/> is true.</summary>
        /// <param name="condition">condition where exception is thrown</param>
        /// <param name="message">message of exception</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(CHECK_DATA)]
        public static void ThrowInvalidDataIf(bool condition, string message)
        {
            if(condition) {
                throw new InvalidDataException(message);
            }
        }

        /// <summary>Throw <see cref="FormatException"/> if <paramref name="condition"/> is true.</summary>
        /// <param name="condition">condition where exception is thrown</param>
        /// <param name="message">message of exception</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(CHECK_DATA)]
        public static void ThrowFormatIf(bool condition, string message)
        {
            if(condition) {
                throw new FormatException(message);
            }
        }
    }
}
