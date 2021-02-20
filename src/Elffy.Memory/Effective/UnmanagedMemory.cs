#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elffy.Effective.Unsafes;
using UnmanageUtility;

namespace Elffy.Effective
{
    [DebuggerTypeProxy(typeof(UnmanagedMemoryDebuggerTypeProxy<>))]
    [DebuggerDisplay("{DebugView}")]
    public readonly unsafe struct UnmanagedMemory<T> : IEquatable<UnmanagedMemory<T>> where T : unmanaged
    {
        private readonly object? _keepAlive;
        private readonly IntPtr _ptr;
        private readonly int _length;

        public static UnmanagedMemory<T> Empty => default;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugView => $"UnmanagedMemory<{typeof(T).Name}>[{_length}]";

        public Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MemoryMarshal.CreateSpan(ref Unsafe.AsRef<T>(_ptr.ToPointer()), _length);
        }

        public int Length => _length;

        public bool IsEmpty => _length == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnmanagedMemory(IntPtr ptr, int length, object? keepAlive)
        {
            _ptr = ptr;
            _length = length;
            _keepAlive = keepAlive;
        }

        public void CopyTo(UnmanagedMemory<T> destination)
        {
            Span.CopyTo(destination.Span);
        }

        public T[] ToArray()
        {
            return Span.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnmanagedMemory<T> Slice(int start)
        {
            // start == _length is valid
            if((uint)start > _length) {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            return new UnmanagedMemory<T>(new IntPtr((T*)_ptr + start), _length - start, _keepAlive);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnmanagedMemory<T> Slice(int start, int length)
        {
            // start == _length is valid
            if((uint)start > _length) {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if((uint)length > (uint)(_length - start)) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            return new UnmanagedMemory<T>(new IntPtr((T*)_ptr + start), length, _keepAlive);
        }

        public override string ToString() => DebugView;

        public override bool Equals(object? obj) => obj is UnmanagedMemory<T> memory && Equals(memory);

        public bool Equals(UnmanagedMemory<T> other) => _ptr.Equals(other._ptr) && _length == other._length;

        public override int GetHashCode() => HashCode.Combine(_ptr, _length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UnmanagedMemory<T>(UnsafeRawArray<T> array) => array.AsUnmanagedMemory();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UnmanagedMemory<T>(UnmanagedArray<T> array) => array.AsUnmanagedMemory();
    }

    internal class UnmanagedMemoryDebuggerTypeProxy<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UnmanagedMemory<T> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _entity.Span.ToArray();

        public UnmanagedMemoryDebuggerTypeProxy(UnmanagedMemory<T> entity)
        {
            _entity = entity;
        }
    }

    public static class UnmanagedMemoryExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnmanagedMemory<T> AsUnmanagedMemory<T>(this UnsafeRawArray<T> source) where T : unmanaged
        {
            return new UnmanagedMemory<T>(source.Ptr, source.Length, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnmanagedMemory<T> AsUnmanagedMemory<T>(this UnsafeRawList<T> source) where T : unmanaged
        {
            return new UnmanagedMemory<T>(source.Ptr, source.Count, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnmanagedMemory<T> AsUnmanagedMemory<T>(this UnmanagedArray<T> source) where T : unmanaged
        {
            // Must keep the instance alive in order not to call the finalizer which releases the pointer.
            return new UnmanagedMemory<T>(source.Ptr, source.Length, source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnmanagedMemory<T> AsUnmanagedMemory<T>(this UnmanagedList<T> source) where T : unmanaged
        {
            // Must keep the instance alive in order not to call the finalizer which releases the pointer.
            return new UnmanagedMemory<T>(source.Ptr, source.Count, source);
        }
    }
}
