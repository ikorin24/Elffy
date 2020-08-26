#nullable enable
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Elffy.Effective.Unsafes
{
#if NET5_0
    public static class ArrayUnsafeExtension
    {
        /// <summary>Get array element at specified index without range checking.</summary>
        /// <remarks>
        /// This method does not check null reference and does not check index range.
        /// *** That means UNDEFINED behaviors may occor in this method. ***
        /// </remarks>
        /// <typeparam name="T">type of element</typeparam>
        /// <param name="source">source array</param>
        /// <param name="index">index of array</param>
        /// <returns>element in the array</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T At<T>(this T[] source, int index)
        {
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(source), index);
        }
    }
#endif
}
