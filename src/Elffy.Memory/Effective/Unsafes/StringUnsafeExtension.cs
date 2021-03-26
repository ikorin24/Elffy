#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Effective.Unsafes
{
    internal static class StringUnsafeExtension
    {
        /// <summary>Get reference to first char of <see cref="string"/></summary>
        /// <param name="source">source <see cref="string"/></param>
        /// <returns>reference to first char</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref char GetReference(this string source)
        {
            return ref Unsafe.AsRef(in source.GetPinnableReference());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> AsSpanUnsafe(this string source, int start)
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref source.GetReference(), start), source.Length - start);
        }

        /// <summary>Get span of <see cref="string"/>.</summary>
        /// <remarks>[CAUTION] This method DOES NOT check boundary !! Be careful !!</remarks>
        /// <param name="source">source <see cref="string"/></param>
        /// <param name="start">start index to slice</param>
        /// <param name="length">length to slice</param>
        /// <returns>sliced span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> AsSpanUnsafe(this string source, int start, int length)
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref source.GetReference(), start), length);
        }

        /// <summary>Get slice of <see cref="ReadOnlySpan{T}"/> of type <see cref="char"/>.</summary>
        /// <remarks>[CAUTION] This method DOES NOT check boundary !! Be careful !!</remarks>
        /// <param name="source">source <see cref="ReadOnlySpan{T}"/> of type <see cref="char"/></param>
        /// <param name="start">start index to slice</param>
        /// <returns>sliced span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> SliceUnsafe(this ReadOnlySpan<char> source, int start)
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(source.At(start)), source.Length - start);
        }

        /// <summary>Get slice of <see cref="ReadOnlySpan{T}"/> of type <see cref="char"/>.</summary>
        /// <remarks>[CAUTION] This method DOES NOT check boundary !! Be careful !!</remarks>
        /// <param name="source">source <see cref="ReadOnlySpan{T}"/> of type <see cref="char"/></param>
        /// <param name="start">start index to slice</param>
        /// <param name="length">length to slice</param>
        /// <returns>sliced span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> SliceUnsafe(this ReadOnlySpan<char> source, int start, int length)
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(source.At(start)), length);
        }
    }
}
