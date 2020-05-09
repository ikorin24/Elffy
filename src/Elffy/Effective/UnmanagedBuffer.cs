#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Elffy.Exceptions;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elffy.Effective
{
    /// <summary>Disposable <see cref="Span{T}"/> of unmanaged heap memory.</summary>
    /// <typeparam name="T">Type of element</typeparam>
    [DebuggerTypeProxy(typeof(UnmanagedBufferDebuggerTypeProxy<>))]
    [DebuggerDisplay("UnmanagedBuffer<{typeof(T).Name}>[{Length}]")]
    public unsafe readonly struct UnmanagedBuffer<T> : IDisposable, IEquatable<UnmanagedBuffer<T>> where T : unmanaged
    {
        private readonly IntPtr _pointer;
        private readonly int _length;

        /// <summary>
        /// Get length of buffer<para/>
        /// [NOTE] Don't use this property as for-loop condition because of performance. Use <see cref="Span{T}.Length"/> instead.<para/>
        /// </summary>
        public readonly int Length => _length;

        /// <summary>Get pointer of buffer head</summary>
        public readonly T* Ptr => (T*)_pointer;

        /// <summary>Get <see cref="Span{T}"/></summary>
        /// <returns><see cref="Span{T}"/></returns>
        public readonly Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Span<T>((T*)_pointer, _length);
        }

        /// <summary>
        /// Allocate unmanaged heap memory of specified length.<para/>
        /// [NOTE] You MUST call <see cref="Dispose"/> to release memory !!! Or memory leak occurs.
        /// </summary>
        /// <param name="length">element length (not bytes length)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnmanagedBuffer(int length)
        {
            ArgumentChecker.ThrowOutOfRangeIf(length <= 0);
            _pointer = Marshal.AllocHGlobal(sizeof(T) * length);
            _length = length;
        }

        /// <summary>Release memory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Marshal.FreeHGlobal(_pointer);
            Unsafe.AsRef(_pointer) = IntPtr.Zero;
            Unsafe.AsRef(_length) = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is UnmanagedBuffer<T> buffer && Equals(buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(UnmanagedBuffer<T> other) => EqualityComparer<IntPtr>.Default.Equals((IntPtr)_pointer, (IntPtr)other._pointer) && _length == other._length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(_pointer, _length);
    }

    internal sealed class UnmanagedBufferDebuggerTypeProxy<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UnmanagedBuffer<T> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public unsafe T[] Items
        {
            get
            {
                var items = new T[_entity.Length];
                for(int i = 0; i < items.Length; i++) {
                    items[i] = _entity.Ptr[i];
                }
                return items;
            }
        }

        public UnmanagedBufferDebuggerTypeProxy(UnmanagedBuffer<T> entity) => _entity = entity;
    }
}
