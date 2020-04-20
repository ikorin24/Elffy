#nullable enable
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Elffy.Effective
{
    /// <summary><see cref="ArrayPool{T}.Shared"/> のヘルパー構造体</summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct PooledArray<T> : IDisposable
    {
        private readonly T[] _array;
        public readonly int Length;

        public readonly T[] InnerArray => _array;

        public readonly bool IsDisposed => _array is null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary><see cref="ArrayPool{T}.Shared"/> から配列バッファを取得します</summary>
        /// <param name="length">配列の長さ</param>
        public PooledArray(int length)
        {
            if(length < 0) { throw new ArgumentOutOfRangeException(nameof(length)); }
            _array = ArrayPool<T>.Shared.Rent(length);
            Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledArray(ReadOnlySpan<T> source)
        {
            _array = ArrayPool<T>.Shared.Rent(source.Length);
            Length = source.Length;
            source.CopyTo(_array.AsSpan(0, Length));
        }

        /// <summary><see cref="Span{T}"/> を取得します</summary>
        /// <returns><see cref="Span{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan()
        {
            return (_array is null == false) ? _array.AsSpan(0, Length) : throw DisposedException();
        }

        /// <summary><see cref="Memory{T}"/> を取得します</summary>
        /// <returns><see cref="Memory{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Memory<T> AsMemory()
        {
            return (_array is null == false) ? _array.AsMemory(0, Length) : throw DisposedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T[] ToArray()
        {
            if(_array is null) { throw DisposedException(); }
            var newArray = new T[Length];
            _array.AsSpan(0, Length).CopyTo(newArray);
            return newArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly PooledArray<TTo> Select<TTo>(bool disposeSource, Func<T, TTo> selector)
        {
            if(_array is null) { throw DisposedException(); }
            if(selector is null) { throw new ArgumentNullException(nameof(selector)); }
            var dest = new PooledArray<TTo>(Length);
            try {
                var destSpan = dest.AsSpan();
                var sourceSpan = AsSpan();
                for(int i = 0; i < sourceSpan.Length; i++) {
                    destSpan[i] = selector(sourceSpan[i]);
                }
                return dest;
            }
            catch(Exception) {
                dest.Dispose();
                throw;
            }
            finally {
                if(disposeSource) { Dispose(); }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T FirstOrDefault(bool disposeSource)
        {
            if(_array is null) { throw DisposedException(); }
            try {
                return Length > 0 ? _array[0]: default!;
            }
            finally {
                if(disposeSource) { Dispose(); }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T FirstOrDefault(bool disposeSource, Func<T, bool> selector)
        {
            if(_array is null) { throw DisposedException(); }
            if(selector is null) { throw new ArgumentNullException(nameof(selector)); }
            try {
                foreach(var item in AsSpan()) {
                    if(selector(item)) { return item; }
                }
                return default!;
            }
            finally {
                if(disposeSource) { Dispose(); }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T First(bool disposeSource)
        {
            if(_array is null) { throw DisposedException(); }
            try {
                return Length > 0 ? _array[0] : throw new InvalidOperationException("No elements");
            }
            finally {
                if(disposeSource) { Dispose(); }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T First(bool disposeSource, Func<T, bool> selector)
        {
            if(_array is null) { throw DisposedException(); }
            if(selector is null) { throw new ArgumentNullException(nameof(selector)); }
            try {
                foreach(var item in AsSpan()) {
                    if(selector(item)) { return item; }
                }
            }
            finally {
                if(disposeSource) { Dispose(); }
            }
            throw new InvalidOperationException("No elements");
        }

        /// <summary>
        /// 配列バッファを <see cref="ArrayPool{T}.Shared"/> に返却します。
        /// このメソッドは <see cref="Length"/> を0にします。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
            if(_array is null == false) {
                ArrayPool<T>.Shared.Return(_array);
                Unsafe.AsRef(_array) = null;
                Unsafe.AsRef(Length) = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ObjectDisposedException DisposedException() => new ObjectDisposedException(nameof(PooledArray<T>), "Buffer has been returned to pool.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Span<T>(PooledArray<T> buffer) => buffer.AsSpan();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ReadOnlySpan<T>(PooledArray<T> buffer) => buffer.AsSpan();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Memory<T>(PooledArray<T> buffer) => buffer.AsMemory();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ReadOnlyMemory<T>(PooledArray<T> buffer) => buffer.AsMemory();
    }
}
