#nullable enable
using Elffy.AssemblyServices;
using System;
using System.Buffers;
using System.Collections.Generic;
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
    public readonly struct PooledArray<T> :
        IFromEnumerable<PooledArray<T>, T>,
        IFromReadOnlySpan<PooledArray<T>, T>,
        ISpan<T>,
        IDisposable
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

        private PooledArray(T[] pooledArray, int usedLength)
        {
            _array = pooledArray;
            Length = usedLength;
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

        public static PooledArray<T> From(IEnumerable<T> source)
        {
            ArgumentNullException.ThrowIfNull(source);
            switch(source) {
                case T[] array: {
                    return new PooledArray<T>(array.AsSpan());
                }
                case List<T> list: {
                    return new PooledArray<T>(list.AsReadOnlySpan());
                }
                case ICollection<T> collection: {
                    return KnownCountEnumerate(collection.Count, collection);
                }
                case IReadOnlyCollection<T> collection: {
                    return KnownCountEnumerate(collection.Count, collection);
                }
                default: {
                    var pooledArray = FromEnumerableImplHelper.EnumerateCollectToPooledArray(source, out var usedLength);
                    return new PooledArray<T>(pooledArray, usedLength);
                }
            }

            static PooledArray<T> KnownCountEnumerate(int count, IEnumerable<T> source)
            {
                var instance = new PooledArray<T>(count);
                var span = instance.AsSpan();
                try {
                    var i = 0;
                    foreach(var item in source) {
                        span[i++] = item;
                    }
                }
                catch {
                    instance.Dispose();
                    throw;
                }
                return instance;
            }
        }

        public static PooledArray<T> From(ReadOnlySpan<T> span) => new PooledArray<T>(span);

        [DoesNotReturn]
        private static void ThrowDisposedException() => throw new ObjectDisposedException(nameof(PooledArray<T>), "Buffer has been returned to pool.");

        public ReadOnlySpan<T> AsReadOnlySpan() => AsSpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Span<T>(PooledArray<T> buffer) => buffer.AsSpan();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ReadOnlySpan<T>(PooledArray<T> buffer) => buffer.AsSpan();
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
