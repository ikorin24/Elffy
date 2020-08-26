#nullable enable
using System.Runtime.CompilerServices;
using System;
using Elffy.AssemblyServices;
#if NETCOREAPP3_1
using System.Runtime.InteropServices;
#endif

namespace Elffy.Effective.Unsafes
{
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
#if NETCOREAPP3_1
        [CriticalDotnetDependency("netcoreapp3.1")]
#endif
#if !(NET5_0 || NETCOREAPP3_1)
        [Obsolete("This method can be used only netcoreapp3.1 or after net5.0 ", true)]
#endif
        public static ref T At<T>(this T[] source, int index)
        {
#if NET5_0 || NETCOREAPP3_1
            return ref Unsafe.Add(ref GetArrayDataReference(source), index);
#else
            throw new NotSupportedException();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP3_1
        [CriticalDotnetDependency("netcoreapp3.1")]
#endif
#if !(NET5_0 || NETCOREAPP3_1)
        [Obsolete("This method can be used only netcoreapp3.1 or after net5.0 ", true)]
#endif
        public static ref T GetArrayDataReference<T>(T[] array)
        {
#if NET5_0
            return MemoryMarshal.GetArrayDataReference(source);
#elif NETCOREAPP3_1
            return ref Unsafe.As<byte, T>(ref Unsafe.As<ArrayDummy>(array).Data);
#else
            throw new NotSupportedException();
#endif
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
