#nullable enable
using System.Runtime.CompilerServices;

namespace Elffy.Effective
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
    }
}
