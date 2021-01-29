#nullable enable
using System.Runtime.CompilerServices;

namespace Elffy.Effective.Unsafes
{
    /// <summary>Helper class of <see cref="Unsafe"/></summary>
    public static class UnsafeEx
    {
        /// <summary>
        /// Casts given readonly reference to a readonly reference to a value of type <typeparamref name="TTo"/>.
        /// </summary>
        /// <remarks>This is compatible with `ref TTo Unsafe.As&lt;TFrom, TTo&gt;(ref TFrom)`</remarks>
        /// <typeparam name="TFrom">source type</typeparam>
        /// <typeparam name="TTo">destination type</typeparam>
        /// <param name="source">source reference</param>
        /// <returns>readonly reference to a value of type <typeparamref name="TTo"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly TTo As<TFrom, TTo>(in TFrom source)
        {
            return ref Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(source));
        }

        /// <summary>Determines whether the specified references point to the same location.</summary>
        /// <typeparam name="T">The type of reference.</typeparam>
        /// <param name="left">The first reference to compare.</param>
        /// <param name="right">The second reference to compare.</param>
        /// <returns>true if left and right point to the same location; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreSame<T>(in T left, in T right)
        {
            return Unsafe.AreSame(ref Unsafe.AsRef(left), ref Unsafe.AsRef(right));
        }

        public static unsafe void* AsPointer<T>(in T value)
        {
            return Unsafe.AsPointer(ref Unsafe.AsRef(in value));
        }
    }
}
