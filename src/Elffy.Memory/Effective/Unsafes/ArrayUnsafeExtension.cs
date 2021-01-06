#nullable enable
using System.Runtime.CompilerServices;
using System;
using System.Runtime.InteropServices;
#if NETCOREAPP3_1
using Elffy.AssemblyServices;
#endif

namespace Elffy.Effective.Unsafes
{
    public static class ArrayUnsafeExtension
    {
        /// <summary>Get array element at specified index without range checking.</summary>
        /// <remarks>
        /// [NOTE] 
        /// This method does not check null reference and does not check index range.
        /// *** That means UNDEFINED behaviors may occor in this method. ***
        /// </remarks>
        /// <typeparam name="T">type of element</typeparam>
        /// <param name="source">source array</param>
        /// <param name="index">index of array</param>
        /// <returns>element in the array</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP3_1
        [CriticalDotnetDependency("netcoreapp3.1")]
#endif
#if !(NET5_0 || NETCOREAPP3_1)
        [Obsolete("This method can be used only netcoreapp3.1 or after net5.0 ", true)]
#endif
        public static ref T At<T>(this T[] source, int index)
        {
#if NET5_0 || NETCOREAPP3_1
            return ref Unsafe.Add(ref GetReference(source), index);
#else
            throw new NotSupportedException();
#endif
        }

        /// <summary>Get reference to the 0th element without any checking.</summary>
        /// <remarks>
        /// [NOTE] DOES NOT use this method for empty array.
        /// </remarks>
        /// <typeparam name="T">type of element</typeparam>
        /// <param name="array">source array</param>
        /// <returns>reference to the 0th element</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP3_1
        [CriticalDotnetDependency("netcoreapp3.1")]
#endif
#if !(NET5_0 || NETCOREAPP3_1)
        [Obsolete("This method can be used only netcoreapp3.1 or after net5.0 ", true)]
#endif
        public static ref T GetReference<T>(this T[] array)
        {
#if NET5_0
            return ref MemoryMarshal.GetArrayDataReference(array);
#elif NETCOREAPP3_1
            return ref Unsafe.As<byte, T>(ref Unsafe.As<ArrayDummy>(array).Data);
#else
            throw new NotSupportedException();
#endif
        }

        /// <summary>Create span without any checking.</summary>
        /// <typeparam name="T">type of element</typeparam>
        /// <param name="array">source array</param>
        /// <returns><see cref="Span{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP3_1
        [CriticalDotnetDependency("netcoreapp3.1")]
#endif
#if !(NET5_0 || NETCOREAPP3_1)
        [Obsolete("This method can be used only netcoreapp3.1 or after net5.0 ", true)]
#endif
        public static Span<T> AsSpanUnsafe<T>(this T[] array)
        {
            return MemoryMarshal.CreateSpan(ref GetReference(array), array.Length);
        }

        /// <summary>Create span without any checking.</summary>
        /// <typeparam name="T">type of element</typeparam>
        /// <param name="array">source array</param>
        /// <param name="start">start index</param>
        /// <returns><see cref="Span{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP3_1
        [CriticalDotnetDependency("netcoreapp3.1")]
#endif
#if !(NET5_0 || NETCOREAPP3_1)
        [Obsolete("This method can be used only netcoreapp3.1 or after net5.0 ", true)]
#endif
        public static Span<T> AsSpanUnsafe<T>(this T[] array, int start)
        {
            return MemoryMarshal.CreateSpan(ref array.At(start), array.Length - start);
        }

        /// <summary>Create span without any checking.</summary>
        /// <typeparam name="T">type of element</typeparam>
        /// <param name="array">source array</param>
        /// <param name="start">start index</param>
        /// <param name="length">length of span</param>
        /// <returns><see cref="Span{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP3_1
        [CriticalDotnetDependency("netcoreapp3.1")]
#endif
#if !(NET5_0 || NETCOREAPP3_1)
        [Obsolete("This method can be used only netcoreapp3.1 or after net5.0 ", true)]
#endif
        public static Span<T> AsSpanUnsafe<T>(this T[] array, int start, int length)
        {
            return MemoryMarshal.CreateSpan(ref array.At(start), length);
        }

#if NETCOREAPP3_1
        private class ArrayDummy
        {
#pragma warning disable 0169    // disable not used warning
            private IntPtr _length;  // int Length (length is int32 but padding exists after it in 32bit runtime.)
#pragma warning restore 0169    // disable not used warning
            internal byte Data;
        }
#endif
    }
}
