#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Elffy.Effective.Unsafes
{
    /// <summary>Variable-length array on unmanaged memory. (like <see cref="List{T}"/>)</summary>
    /// <remarks>
    /// [NOTE] if you use debugger viewer, enable zero-initialized at constructor. (otherwise shows random values or throws an exception in debugger.)
    /// </remarks>
    /// <typeparam name="T">type of element</typeparam>
    [DebuggerTypeProxy(typeof(UnsafeRawListDebuggerTypeProxy<>))]
    [DebuggerDisplay("UnsafeRawList<{typeof(T).Name}>[{Count}]")]
    public unsafe struct UnsafeRawList<T> : IDisposable where T : unmanaged
    {
        private UnsafeRawArray<T> _array;
        private int _count;

        /// <summary>Get count of element</summary>
        public readonly int Count => _count;

        /// <summary>Get capacity of the inner array</summary>
        public int Capacity
        {
            readonly get => _array.Length;
            set
            {
                if(value < _count) {
                    ThrowOutOfRange();
                    static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value));
                }
                if(value != _count) {
                    var newArray = new UnsafeRawArray<T>(value, false);
                    if(_count > 0) {
                        try {
                            AsSpan().CopyTo(newArray.AsSpan());
                        }
                        catch {
                            newArray.Dispose();
                            throw;
                        }
                        _array.Dispose();
                    }
                    _array = newArray;
                }
            }
        }

        /// <summary>Get pointer to the head</summary>
        public readonly IntPtr Ptr => _array.Ptr;

        /// <summary>Get or set an element of specified index. (Boundary is not checked, be careful.)</summary>
        /// <param name="index">index of the element</param>
        /// <returns>an element of specified index</returns>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[index];
        }

        /// <summary>Allocate new list of spicified capacity. ()</summary>
        /// <param name="capacity">capacity of the inner array</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeRawList(int capacity)
        {
            _array = new UnsafeRawArray<T>(capacity, zeroFill: false);
            _count = 0;
        }

        /// <summary>Allocate new list of spicified capacity. ()</summary>
        /// <param name="capacity">capacity of the inner array</param>
        /// <param name="zeroFill">Whether to initialized the inner array by zero.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeRawList(int capacity, bool zeroFill)
        {
            _array = new UnsafeRawArray<T>(capacity, zeroFill);
            _count = 0;
        }

        /// <summary>Add specified item to tail of the list.</summary>
        /// <param name="item">item to add</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T item)
        {
            if(_count >= _array.Length) {
                Extend();   // Never inlined for performance because uncommon path.
            }
            _array[_count] = item;
            _count++;
        }

        /// <summary>Get index of specified item. Return -1 if not contained.</summary>
        /// <param name="item">item to get index</param>
        /// <returns>index of the item</returns>
        public int IndexOf(in T item)
        {
            for(int i = 0; i < _count; i++) {
                if(EqualityComparer<T>.Default.Equals(_array[i], item)) {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Free alocated memory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _array.Dispose();
            _count = 0;
        }

        /// <summary>Copy to managed memory</summary>
        /// <param name="array">managed memory array</param>
        /// <param name="arrayIndex">start index of destination array</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if(array == null) { throw new ArgumentNullException(nameof(array)); }
            if((uint)arrayIndex >= (uint)array.Length) { throw new ArgumentOutOfRangeException(nameof(arrayIndex)); }
            if(arrayIndex + _count > array.Length) { throw new ArgumentException("There is not enouph length of destination array"); }

            if(_count == 0) {
                return;
            }

            fixed(T* arrayPtr = array) {
                var byteLen = (long)(_count * sizeof(T));
                Buffer.MemoryCopy((void*)Ptr, arrayPtr + arrayIndex, byteLen, byteLen);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan()
        {
            return _array.AsSpan(0, _count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan(int start)
        {
            return _array.AsSpan(start, _count - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan(int start, int length)
        {
            return _array.AsSpan(start, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path
        private void Extend()
        {
            var newArray = new UnsafeRawArray<T>(_array.Length == 0 ? 4 : _array.Length * 2);
            try {
                Buffer.MemoryCopy(_array.Ptr.ToPointer(), newArray.Ptr.ToPointer(), newArray.Length * sizeof(T), _array.Length * sizeof(T));
            }
            catch {
                newArray.Dispose();
                throw;
            }
            _array.Dispose();
            _array = newArray;
        }
    }

    internal class UnsafeRawListDebuggerTypeProxy<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UnsafeRawList<T> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var items = new T[_entity.Count];
                _entity.CopyTo(items, 0);
                return items;
            }
        }

        public UnsafeRawListDebuggerTypeProxy(UnsafeRawList<T> entity) => _entity = entity;
    }
}
