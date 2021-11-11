#nullable enable
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Elffy.AssemblyServices;
#if NET5_0_OR_GREATER
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP3_1
        [CriticalDotnetDependency("netcoreapp3.1")]
#endif
        public static Span<T> AsSpan<T>(this List<T>? list)
        {
#if NETCOREAPP3_1
            return (list is null) ? default : Unsafe.As<ListDummy<T>>(list)._items.AsSpan(0, list.Count);
#elif NET5_0_OR_GREATER
            return CollectionsMarshal.AsSpan(list);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP3_1
        [CriticalDotnetDependency("netcoreapp3.1")]
#endif
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this List<T>? list) => list.AsSpan();

        /// <summary>Clear <see cref="List{T}"/> and get the data as <see cref="Span{T}"/>.</summary>
        /// <remarks>Capacity of <see cref="List{T}"/> gets 0 after calling this method.</remarks>
        /// <typeparam name="T">type of elements</typeparam>
        /// <param name="list">source object</param>
        /// <returns>cleared data as <see cref="Span{T}"/> in <see cref="List{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CriticalDotnetDependency("netcoreapp3.1 || net5.0")]
        public static Span<T> ClearWithExtracting<T>(this List<T> list)
        {
            var dummy = Unsafe.As<ListDummy<T>>(list);
            var items = dummy._items.AsSpan(0, dummy._size);
            dummy._version++;
            dummy._items = Array.Empty<T>();
            dummy._size = 0;
            return items;
        }

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
