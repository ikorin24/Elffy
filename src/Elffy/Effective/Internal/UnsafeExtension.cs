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
        internal static Span<T> ExtractInnerArray<T>(this ReadOnlyCollection<T> collection) // TODO: Test this method on each .NET version
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
                throw new InvalidCastException($"Inner field of {nameof(ReadOnlyCollection<T>)} cannot be implemented.\r\nType : {dummy.List.GetType()}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Memory<T> AsMemory<T>(this List<T> list) => Unsafe.As<ListDummy<T>>(list).Item.AsMemory();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Span<T> AsSpan<T>(this List<T> list) => Unsafe.As<ListDummy<T>>(list).Item.AsSpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlyMemory<T> AsReadOnlyMemory<T>(this List<T> list) => list.AsMemory();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<T> AsReadOnlySpan<T>(this List<T> list) => list.AsSpan();

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
            internal T[] Item = default!;
            private ListDummy() { }
        }
    }
}
