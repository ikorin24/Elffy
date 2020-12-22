#nullable enable
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Elffy.AssemblyServices;
#if NET5_0
using System.Runtime.InteropServices;
#endif

namespace Elffy.Effective
{
    public static class ListExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CriticalDotnetDependency("netcoreapp3.1 || net5.0")]
        public static void AddRange<T>(this List<T> list, ReadOnlySpan<T> span)
        {
            if(span.IsEmpty) { return; }

            if(list.Capacity < list.Count + span.Length) {
                int cap = list.Capacity + span.Length;
                if(cap > (1 << 30)) {
                    const int MaxArrayLength = 0X7FEFFFFF;      // This size is compatible with Array.MaxArrayLength. (This is internal)
                    cap = MaxArrayLength;
                }
                else {
                    if(cap == 0) {
                        cap = 4;
                    }
                    // round up to power of two
                    cap = (1 << (32 - BitOperations.LeadingZeroCount((uint)cap - 1)));
                }
                list.Capacity = cap;
            }
            var dest = Unsafe.As<ListDummy<T>>(list)._items.AsSpan(list.Count);
            span.CopyTo(dest);
            Unsafe.As<ListDummy<T>>(list)._size += span.Length;
            Unsafe.As<ListDummy<T>>(list)._version++;
        }

        [CriticalDotnetDependency("netcoreapp3.1 || net5.0")]
        public static void InsertRange<T>(this List<T> list, int index, ReadOnlySpan<T> span)
        {
            if(list is null) { throw new ArgumentNullException(nameof(list)); }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP3_1
        [CriticalDotnetDependency("netcoreapp3.1")]
#endif
        public static Span<T> AsSpan<T>(this List<T> list)
        {
#if NETCOREAPP3_1
            return (list is null) ? default : Unsafe.As<ListDummy<T>>(list)._items.AsSpan(0, list.Count);
#elif NET5_0
            return CollectionsMarshal.AsSpan(list);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP3_1
        [CriticalDotnetDependency("netcoreapp3.1")]
#endif
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this List<T> list) => list.AsSpan();


        private abstract class ListDummy<T>
        {
            internal T[] _items = default!;
            internal int _size;
            internal int _version;
            //internal object _syncRoot = default!;     // for NETFRAMEWORK

            private ListDummy() { }
        }
    }
}
