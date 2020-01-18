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
        /// ************** [NOTICE] **************                                                  <para/>
        /// This is VERY UNSAFE !!!!                                                                <para/>
        /// The method may depend on version of .NET or mono !                                      <para/>
        /// See more info in the comment of the method or <see cref="ReadOnlyCollectionDummy{T}"/>. <para/>
        /// - - - - - - - - - - - - - - - - - - - -                                                 <para/>
        /// Arg must be a wrapper of T[] or List(T). NOT any other IList(T).                        <para/>
        /// This is not related to the above restriction.                                           <para/>
        /// </summary>
        /// <typeparam name="T">type of item</typeparam>
        /// <param name="collection"><see cref="ReadOnlyCollection{T}"/></param>
        /// <returns>Inner array of <see cref="ReadOnlyCollection{T}"/></returns>
        internal static Span<T> ExtractInnerArray<T>(this ReadOnlyCollection<T> collection) // TODO: Test this method on each .NET version
        {
            if(collection == null) { throw new ArgumentNullException(nameof(collection)); }

            // **********************************************************
            // Apply force cast ReadOnlyCollection<T> into dummy.
            // Layout of the dummy class must be same as ReadOnlyCollection<T>.
            // If not, the memory becomes crashed and nobody knows what will happen !!
            // **********************************************************

            var dummy = Unsafe.As<ReadOnlyCollectionDummy<T>>(collection);

            // The memory already has crashed here If the above code is invalid.
            // If it becomes in the case, the following code may fly away.  Oh Nooooooooo  :_(

            if(dummy.List is T[] array) {
                return array.AsSpan();
            }
            //else if(dummy.List is List<T> list) {
            //    return list.ExtractInnerArray();
            //}
            else {
                throw new InvalidCastException($"Inner field of {nameof(ReadOnlyCollection<T>)} cannot be implemented.\r\nType : {dummy.List.GetType()}");
            }
        }

        /*

        /// <summary>
        /// ************** [NOTICE] **************                                                  <para/>
        /// This is VERY UNSAFE !!!!                                                                <para/>
        /// The method may depend on version of .NET or mono !                                      <para/>
        /// See more info in the comment of the method or <see cref="ListDummy{T}{T}"/>.            <para/>
        /// </summary>
        /// <typeparam name="T">type of item</typeparam>
        /// <param name="collection"><see cref="List{T}"/></param>
        /// <returns>Inner array of <see cref="List{T}"/></returns>
        internal static Span<T> ExtractInnerArray<T>(this List<T> list)                 // TODO: Test this method on each .NET version
        {
            if(list == null) { throw new ArgumentNullException(nameof(list)); }

            // **********************************************************
            // Apply force cast List<T> into dummy.
            // Layout of the dummy class must be same as List<T>.
            // If not, the memory becomes crashed and nobody knows what will happen !!
            // **********************************************************

            var dummy = Unsafe.As<ListDummy<T>>(list);

            // The memory already has crashed here If the above code is invalid.
            // If it becomes in the case, the following code may fly away.  Oh Nooooooooo  :_(

            var span = dummy.Array.AsSpan(0, dummy.Size);
            return span;
        }

        */

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


        // ListDummy は .NET のバージョンに左右されるのが危険すぎる
        /*


        /// <summary>
        /// ************** [NOTICE] **************                                                  <para/>
        /// This is VERY UNSAFE !!!!                                                                <para/>
        /// The method may depend on version of .NET or mono !                                      <para/>
        /// Layout of this class on the memory MUST be same as <see cref="List{T}"/>                <para/>
        /// This is a dummy class of <see cref="List{T}"/>                                          <para/>
        /// </summary>
        /// <typeparam name="T">item type</typeparam>
        private class ListDummy<T>
        {
#pragma warning disable 0649
#pragma warning disable 0169
#pragma warning disable 0414
            private const int _defaultCapacity = 4;
            public readonly T[] Array = null!;
            public readonly int Size;
            private readonly int _version;
#if NETFRAMEWORK
            private object? _syncRoot;
#endif
            //private static readonly T[]? _emptyArray = null!;
#pragma warning restore 0649
#pragma warning restore 0169
#pragma warning restore 0414
            private ListDummy() { }
        }

        */

    }
}
