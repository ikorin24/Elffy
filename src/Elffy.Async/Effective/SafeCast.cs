#nullable enable
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    /// <summary>Helper class for fast cast</summary>
    internal static class SafeCast
    {
        /// <summary>Call <see cref="Unsafe.As{T}(object?)"/> with type checking by <see cref="Debug.Assert(bool)"/></summary>
        /// <remarks><paramref name="value"/> can be null.</remarks>
        /// <typeparam name="T">type to cast to</typeparam>
        /// <param name="value">object to cast</param>
        /// <returns>casted object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull("value")]
        [DebuggerHidden]
        public static T? As<T>(object? value) where T : class
        {
            Debug.Assert(value is null || value is T);
            return Unsafe.As<T?>(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static T NotNullAs<T>(object? value) where T : class
        {
            Debug.Assert(value is T);
            return Unsafe.As<T>(value);
        }
    }
}
