#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Effective.Unsafes
{
    public static class SpanUnsafeExtension
    {
        /// <summary>Get reference to specified indexed element of <see cref="Span{T}"/></summary>
        /// <remarks>[CAUTION] This method DOES NOT check boundary !! Be careful !!</remarks>
        /// <typeparam name="T">type of element of <see cref="Span{T}"/></typeparam>
        /// <param name="source">source object</param>
        /// <param name="i">index</param>
        /// <returns>reference to element</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T At<T>(this Span<T> source, int i)
        {
            return ref Unsafe.Add(ref MemoryMarshal.GetReference(source), i);
        }

        /// <summary>Get reference to specified indexed element of <see cref="ReadOnlySpan{T}"/></summary>
        /// <remarks>[CAUTION] This method DOES NOT check boundary !! Be careful !!</remarks>
        /// <typeparam name="T">type of element of <see cref="ReadOnlySpan{T}"/></typeparam>
        /// <param name="source">source object</param>
        /// <param name="i">index</param>
        /// <returns>reference to element</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T At<T>(this ReadOnlySpan<T> source, int i)
        {
            return ref Unsafe.Add(ref MemoryMarshal.GetReference(source), i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> SliceUnsafe<T>(this Span<T> source, int start)
        {
            return MemoryMarshal.CreateSpan(ref source.At(start), source.Length - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> SliceUnsafe<T>(this ReadOnlySpan<T> source, int start)
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in source.At(start)), source.Length - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> SliceUnsafe<T>(this Span<T> source, int start, int length)
        {
            return MemoryMarshal.CreateSpan(ref source.At(start), length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> SliceUnsafe<T>(this ReadOnlySpan<T> source, int start, int length)
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in source.At(start)), length);
        }

        /// <summary>
        /// Change <see cref="ReadOnlySpan{T}"/> into <see cref="Span{T}"/>, which is very DENGEROUS !!! Be carefull !!!
        /// </summary>
        /// <typeparam name="T">type of Span</typeparam>
        /// <param name="source">source object as <see cref="ReadOnlySpan{T}"/></param>
        /// <returns><see cref="Span{T}"/> created from <paramref name="source"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsWritable<T>(this ReadOnlySpan<T> source)
            => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(source), source.Length);
    }
}
