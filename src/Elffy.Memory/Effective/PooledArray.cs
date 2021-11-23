#nullable enable
using Elffy.AssemblyServices;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.Effective
{
    /// <summary>Helper sturct of <see cref="ArrayPool{T}.Shared"/></summary>
    /// <typeparam name="T">element type</typeparam>
    [DebuggerTypeProxy(typeof(PooledArrayDebuggerTypeProxy<>))]
    [DebuggerDisplay("PooledArray<{typeof(T).Name,nq}>[{Length}]")]
    [DontUseDefault]
    public readonly struct PooledArray<T> : IDisposable
    {
        private readonly T[] _array;
        public readonly int Length;

        public readonly T[] InnerArray => _array;

        public readonly bool IsDisposed => _array is null;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public PooledArray() => throw new NotSupportedException("Don't use default constructor.");

        /// <summary>Get buffer from <see cref="ArrayPool{T}.Shared"/></summary>
        /// <param name="length">length of the buffer</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledArray(int length)
        {
            if(length < 0) { throw new ArgumentOutOfRangeException(nameof(length)); }
            _array = ArrayPool<T>.Shared.Rent(length);
            Length = length;
        }

        /// <summary>Get buffer from <see cref="ArrayPool{T}.Shared"/> and fill it by the specified <paramref name="source"/>.</summary>
        /// <param name="source">source span</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledArray(ReadOnlySpan<T> source)
        {
            _array = ArrayPool<T>.Shared.Rent(source.Length);
            Length = source.Length;
            source.CopyTo(_array.AsSpan(0, Length));
        }

        /// <summary>Get <see cref="Span{T}"/></summary>
        /// <returns><see cref="Span{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan()
        {
            if(_array is null) { ThrowDisposedException(); }
            return _array.AsSpan(0, Length);
        }

        /// <summary>Get <see cref="Span{T}"/></summary>
        /// <param name="start">start index</param>
        /// <returns><see cref="Span{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan(int start)
        {
            if(_array is null) { ThrowDisposedException(); }
            if((uint)start >= (uint)Length) { throw new ArgumentOutOfRangeException(nameof(start)); }
            return _array.AsSpan(start, Length - start);
        }

        /// <summary>Get <see cref="Span{T}"/></summary>
        /// <param name="start">start index</param>
        /// <param name="length">length of span</param>
        /// <returns><see cref="Span{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan(int start, int length)
        {
            if(_array is null) { ThrowDisposedException(); }
            if((uint)start >= (uint)Length) { throw new ArgumentOutOfRangeException(nameof(start)); }
            if((uint)length > (uint)(Length - start)) { throw new ArgumentOutOfRangeException(nameof(length)); }
            return _array.AsSpan(start, length);
        }

        /// <summary>Get <see cref="Memory{T}"/></summary>
        /// <returns><see cref="Memory{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Memory<T> AsMemory()
        {
            if(_array is null) { ThrowDisposedException(); }
            return _array.AsMemory(0, Length);
        }

        /// <summary>Get <see cref="Memory{T}"/></summary>
        /// <param name="start">start index</param>
        /// <returns><see cref="Memory{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Memory<T> AsMemory(int start)
        {
            if(_array is null) {
                ThrowDisposedException();
            }
            return _array.AsMemory(start, Length - start);
        }

        /// <summary>Get <see cref="Memory{T}"/></summary>
        /// <param name="start">start index</param>
        /// <param name="length">length of memory</param>
        /// <returns><see cref="Memory{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Memory<T> AsMemory(int start, int length)
        {
            if(_array is null) {
                ThrowDisposedException();
            }
            return _array.AsMemory(start, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T[] ToArray()
        {
            if(_array is null) {
                ThrowDisposedException();
            }
            var newArray = new T[Length];
            _array.AsSpan(0, Length).CopyTo(newArray);
            return newArray;
        }

        /// <summary>Returns the inner array to the array pool.</summary>
        /// <remarks>The method make <see cref="Length"/> 0.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
            if(_array is null == false) {
                ArrayPool<T>.Shared.Return(_array);
                Unsafe.AsRef(_array) = null!;
                Unsafe.AsRef(Length) = 0;
            }
        }

        [DoesNotReturn]
        private static void ThrowDisposedException() => throw new ObjectDisposedException(nameof(PooledArray<T>), "Buffer has been returned to pool.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Span<T>(PooledArray<T> buffer) => buffer.AsSpan();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ReadOnlySpan<T>(PooledArray<T> buffer) => buffer.AsSpan();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Memory<T>(PooledArray<T> buffer) => buffer.AsMemory();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ReadOnlyMemory<T>(PooledArray<T> buffer) => buffer.AsMemory();
    }

    internal sealed class PooledArrayDebuggerTypeProxy<T> where T : class?
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly T[] _items;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _items;

        internal PooledArrayDebuggerTypeProxy(PooledArray<T> entity)
        {
            _items = entity.ToArray();
        }
    }
}
