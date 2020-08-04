#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elffy.AssemblyServices;

namespace Elffy.Effective.Unsafes
{
    internal static class UnsafeExtension
    {
        /// <summary>
        /// Change <see cref="ReadOnlySpan{T}"/> into <see cref="Span{T}"/>, which is very DENGEROUS !!! Be carefull !!!
        /// </summary>
        /// <typeparam name="T">type of Span</typeparam>
        /// <param name="source">source object as <see cref="ReadOnlySpan{T}"/></param>
        /// <returns><see cref="Span{T}"/> created from <paramref name="source"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Span<T> AsWritable<T>(this ReadOnlySpan<T> source)
            => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(source), source.Length);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CriticalDotnetDependency("netcoreapp3.1")]
        internal static Memory<T> AsMemory<T>(this List<T> list) => Unsafe.As<ListDummy<T>>(list)._items.AsMemory(0, list.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CriticalDotnetDependency("netcoreapp3.1")]
        internal static Span<T> AsSpan<T>(this List<T> list) => Unsafe.As<ListDummy<T>>(list)._items.AsSpan(0, list.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CriticalDotnetDependency("netcoreapp3.1")]
        internal static ReadOnlyMemory<T> AsReadOnlyMemory<T>(this List<T> list) => list.AsMemory();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CriticalDotnetDependency("netcoreapp3.1")]
        internal static ReadOnlySpan<T> AsReadOnlySpan<T>(this List<T> list) => list.AsSpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CriticalDotnetDependency("netcoreapp3.1")]
        internal static void AddRange<T>(this List<T> list, ReadOnlySpan<T> span) => list.InsertRange(list.Count, span);

        [CriticalDotnetDependency("netcoreapp3.1")]
        internal static void InsertRange<T>(this List<T> list, int index, ReadOnlySpan<T> span)
        {
            if(list == null) { throw new ArgumentNullException(nameof(list)); }
            static void EnsureCapacity(List<T> original, ListDummy<T> dummy, int min)
            {
                if(dummy._items.Length < min) {
                    const int DefaultCapacity = 4;
                    int newCapacity = dummy._items.Length == 0 ? DefaultCapacity : dummy._items.Length * 2;

                    // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                    // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                    const int MaxArrayLength = 0X7FEFFFFF;      // This size is compatible with Array.MaxArrayLength. (This is internal)
                    if((uint)newCapacity > MaxArrayLength) newCapacity = MaxArrayLength;
                    if(newCapacity < min) newCapacity = min;
                    original.Capacity = newCapacity;
                }
            }

            var dummyList = Unsafe.As<ListDummy<T>>(list);
            if((uint)index > (uint)dummyList._size) {
                throw new ArgumentOutOfRangeException();
            }
            int count = span.Length;
            if(count > 0) {
                EnsureCapacity(list, dummyList, dummyList._size + count);
                if(index < dummyList._size) {
                    Array.Copy(dummyList._items, index, dummyList._items, index + count, dummyList._size - index);
                }

                span.CopyTo(dummyList._items.AsSpan(index));
                dummyList._size += count;
                dummyList._version++;
            }
        }


        private abstract class ListDummy<T>
        {
            internal T[] _items = default!;
            internal int _size;
            internal int _version;
#if NETFRAMEWORK
            internal object _syncRoot = default!;
#endif

            private ListDummy() { }
        }
    }
}
