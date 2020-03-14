#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Effective
{
    /// <summary>Variable length array implemented by unmanaged memory.</summary>
    /// <typeparam name="T">Type of element</typeparam>
    [DebuggerTypeProxy(typeof(UnmanagedListDebuggerTypeProxy<>))]
    [DebuggerDisplay("UnmanagedList<{typeof(T).Name}>[{Count}]")]
    public unsafe struct UnmanagedList<T> : IDisposable, IEquatable<UnmanagedList<T>> where T : unmanaged
    {
        private UnmanagedBuffer<T> _array;
        private int _count;

        /// <summary>Get count of elements</summary>
        public readonly int Count => _count;

        /// <summary>
        /// Get <see cref="Span{T}"/><para/>
        /// [NOTE] <see cref="AccessViolationException"/> may happen after inner array changed. Use this property careflly.
        /// </summary>
        /// <returns><see cref="Span{T}"/></returns>
        public Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Span<T>(_array.Ptr, _count);
        }

        /// <summary>Get pointer of a head element</summary>
        public readonly T* Ptr => _array.Ptr;

        /// <summary>
        /// Create variable length array allocated in unmanaged heap memory.<para/>
        /// [NOTE] You MUST call <see cref="Dispose"/> to release memory !!! Or memory leak occurs.
        /// </summary>
        /// <param name="capacity">capacity of inner array</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnmanagedList(int capacity)
        {
            _array = new UnmanagedBuffer<T>(capacity);
            _count = 0;
        }

        /// <summary>Add item to tail</summary>
        /// <param name="item">item</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            EnsureIndexAccess(_count);
            _array.Span[_count] = item;
            _count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(ReadOnlySpan<T> items)
        {
            if(items.Length == 0) { return; }
            EnsureIndexAccess(_count + items.Length - 1);
            items.CopyTo(_array.Span.Slice(_count));
            _count += items.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index)
        {
            var span = _array.Span;
            span.Slice(index + 1, _count - index - 1).CopyTo(span.Slice(index));
            _array.Ptr[_count] = default;
            _count--;
        }

        /// <summary>
        /// Release memory<para/>
        /// [NOTE] You MUST call this after using this instance !!! Or memory leak occurs.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _array.Dispose();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureIndexAccess(int index)
        {
            Debug.Assert(index >= 0);
            var capacity = _array.Length;
            if(capacity <= index) {
                UnmanagedBuffer<T> newArray;
                checked {
                    newArray = new UnmanagedBuffer<T>(capacity < 4 ? 4 : (capacity << 1));
                }
                _array.Span.CopyTo(newArray.Span);
                _array.Dispose();
                _array = newArray;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is UnmanagedList<T> list && Equals(list);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(UnmanagedList<T> other) => _array.Equals(other._array) && _count == other._count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(_array, _count);
    }

    internal sealed class UnmanagedListDebuggerTypeProxy<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UnmanagedList<T> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public unsafe T[] Items
        {
            get
            {
                var items = new T[_entity.Count];
                for(int i = 0; i < items.Length; i++) {
                    items[i] = _entity.Ptr[i];
                }
                return items;
            }
        }

        public UnmanagedListDebuggerTypeProxy(UnmanagedList<T> entity) => _entity = entity;
    }
}
