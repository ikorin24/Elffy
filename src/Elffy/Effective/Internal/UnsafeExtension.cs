#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace Elffy.Effective.Internal
{
    internal static class UnsafeExtension
    {
        /// <summary>
        /// Get <see cref="Span{T}"/> from <see cref="ReadOnlyCollection{T}"/> <para/>
        /// ************** [NOTICE] **************                             <para/>
        /// This is VERY UNSAFE !!!!                                           <para/>
        /// </summary>
        /// <typeparam name="T">type of item</typeparam>
        /// <param name="collection"><see cref="ReadOnlyCollection{T}"/></param>
        /// <returns>Inner array of <see cref="ReadOnlyCollection{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Span<T> AsSpan<T>(this ReadOnlyCollection<T> collection) // TODO: Test this method on each .NET version
        {
            if(collection == null) { throw new ArgumentNullException(nameof(collection)); }
            var dummy = Unsafe.As<ReadOnlyCollectionDummy<T>>(collection);
            if(dummy.List is T[] array) {
                return array.AsSpan();
            }
            else if(dummy.List is List<T> list) {
                return list.AsSpan();
            }
            else {
                throw new InvalidCastException($"Inner field of {nameof(ReadOnlyCollection<T>)} cannot be implemented. Type : {dummy.List.GetType()}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static Span<T> AsWritable<T>(this ReadOnlySpan<T> source) where T : unmanaged
        {
            fixed(T* ptr = source) {
                return new Span<T>(ptr, source.Length);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Memory<T> AsMemory<T>(this List<T> list) => Unsafe.As<ListDummy<T>>(list)._items.AsMemory(0, list.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Span<T> AsSpan<T>(this List<T> list) => Unsafe.As<ListDummy<T>>(list)._items.AsSpan(0, list.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlyMemory<T> AsReadOnlyMemory<T>(this List<T> list) => list.AsMemory();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<T> AsReadOnlySpan<T>(this List<T> list) => list.AsSpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddRange<T>(this List<T> list, ReadOnlySpan<T> span) => list.InsertRange(list.Count, span);

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

        /// <summary>
        /// ************** [NOTICE] **************                                                  <para/>
        /// This is VERY UNSAFE !!!!                                                                <para/>
        /// The method may depend on version of .NET or mono !                                      <para/>
        /// Layout of this class on the memory MUST be same as <see cref="ReadOnlyCollection{T}"/>  <para/>
        /// This is a dummy class of <see cref="ReadOnlyCollection{T}"/>                            <para/>
        /// </summary>
        /// <typeparam name="T">item type</typeparam>
        private class ReadOnlyCollectionDummy<T>
        {
            public readonly IList<T> List = null!;

            private ReadOnlyCollectionDummy() { }
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
