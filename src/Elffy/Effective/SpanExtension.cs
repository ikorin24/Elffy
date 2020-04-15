#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Elffy.Effective
{
    public static class SpanExtension
    {
        /// <summary>
        /// Cast whole <see cref="Span{T}"/> to another type of <see cref="Span{T}"/>.      <para/>
        /// This is not each-element cast. The memory layout is not changed.                <para/>
        /// [example] from <see cref="byte"/> to <see cref="short"/>                        <para/>
        /// from : [ 0x00 | 0x01 | 0x02 | 0x03 ] == Span(byte),  Length=4                   <para/>
        /// to   : [ 0x00   0x01 | 0x02   0x03 ] == Span(short), Length=2                   <para/>
        /// </summary>
        /// <typeparam name="TFrom">type of source <see cref="Span{T}"/> item</typeparam>
        /// <typeparam name="TTo">type of destination <see cref="Span{T}"/> item</typeparam>
        /// <param name="source">source <see cref="Span{T}"/></param>
        /// <returns>destination <see cref="Span{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<TTo> MarshalCast<TFrom, TTo>(this Span<TFrom> source) where TFrom : struct
                                                                                 where TTo : struct
        {
            return MemoryMarshal.Cast<TFrom, TTo>(source);
        }

        /// <summary>
        /// Cast whole <see cref="ReadOnlySpan{T}"/> to another type of <see cref="ReadOnlySpan{T}"/>.  <para/>
        /// This is not each-element cast. The memory layout is not changed.                            <para/>
        /// [example] from <see cref="byte"/> to <see cref="short"/>                                    <para/>
        /// from : [ 0x00 | 0x01 | 0x02 | 0x03 ] == ReadOnlySpan(byte),  Length=4                       <para/>
        /// to   : [ 0x00   0x01 | 0x02   0x03 ] == ReadOnlySpan(short), Length=2                       <para/>
        /// </summary>
        /// <typeparam name="TFrom">type of source <see cref="ReadOnlySpan{T}"/> item</typeparam>
        /// <typeparam name="TTo">type of destination <see cref="ReadOnlySpan{T}"/> item</typeparam>
        /// <param name="source">source <see cref="ReadOnlySpan{T}"/></param>
        /// <returns>destination <see cref="ReadOnlySpan{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<TTo> MarshalCast<TFrom, TTo>(this ReadOnlySpan<TFrom> source) where TFrom : struct
                                                                                                 where TTo : struct
        {
            return MemoryMarshal.Cast<TFrom, TTo>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Span<TTo> AsSpan<TFrom, TTo>(this TFrom source) where TFrom : unmanaged
                                                                             where TTo : unmanaged
        {
            var arrayLen = sizeof(TFrom) / sizeof(TTo) + (sizeof(TFrom) % sizeof(TTo) > 0 ? 1 : 0);
            return new Span<TTo>(&source, arrayLen);
        }


        // TODO: dll パッケージに入れる

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnmanagedArray<T> ToUnmanagedArray<T>(this Span<T> source) where T : unmanaged => ToUnmanagedArray((ReadOnlySpan<T>)source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnmanagedArray<T> ToUnmanagedArray<T>(this ReadOnlySpan<T> source) where T : unmanaged => new UnmanagedArray<T>(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe UnmanagedArray<TTo> SelectToUnmanagedArray<TFrom, TTo>(this Span<TFrom> source, Func<TFrom, TTo> selector) where TTo : unmanaged
            => SelectToUnmanagedArray((ReadOnlySpan<TFrom>)source, selector);

        public static unsafe UnmanagedArray<TTo> SelectToUnmanagedArray<TFrom, TTo>(this ReadOnlySpan<TFrom> source, Func<TFrom, TTo> selector) where TTo : unmanaged
        {
            if(selector == null) { throw new ArgumentNullException(nameof(selector)); }
            var umArray = new UnmanagedArray<TTo>(source.Length);
            try {
                var ptr = (TTo*)umArray.Ptr;
                for(int i = 0; i < source.Length; i++) {
                    ptr[i] = selector(source[i]);
                }
                return umArray;
            }
            catch(Exception) {
                umArray.Dispose();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe UnmanagedArray<TTo> SelectToUnmanagedArray<TFrom, TTo>(this Span<TFrom> source, Func<TFrom, int, TTo> selector) where TTo : unmanaged
            => SelectToUnmanagedArray((ReadOnlySpan<TFrom>)source, selector);

        public static unsafe UnmanagedArray<TTo> SelectToUnmanagedArray<TFrom, TTo>(this ReadOnlySpan<TFrom> source, Func<TFrom, int, TTo> selector) where TTo : unmanaged
        {
            if(selector == null) { throw new ArgumentNullException(nameof(selector)); }
            var umArray = new UnmanagedArray<TTo>(source.Length);
            try {
                var ptr = (TTo*)umArray.Ptr;
                for(int i = 0; i < source.Length; i++) {
                    ptr[i] = selector(source[i], i);
                }
                return umArray;
            }
            catch(Exception) {
                umArray.Dispose();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<TTo> SelectToSpan<TFrom, TTo>(this Span<TFrom> source, Span<TTo> buffer, Func<TFrom, TTo> selector)
            => SelectToSpan((ReadOnlySpan<TFrom>)source, buffer, selector);

        public static Span<TTo> SelectToSpan<TFrom, TTo>(this ReadOnlySpan<TFrom> source, Span<TTo> buffer, Func<TFrom, TTo> selector)
        {
            if(selector == null) { throw new ArgumentNullException(nameof(selector)); }
            if(source.Length > buffer.Length) { throw new ArgumentException($"Length of {nameof(buffer)} is too short."); }
            for(int i = 0; i < source.Length; i++) {
                buffer[i] = selector(source[i]);
            }
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<TTo> SelectToSpan<TFrom, TTo>(this Span<TFrom> source, Span<TTo> buffer, Func<TFrom, int, TTo> selector)
            => SelectToSpan((ReadOnlySpan<TFrom>)source, buffer, selector);

        public static Span<TTo> SelectToSpan<TFrom, TTo>(this ReadOnlySpan<TFrom> source, Span<TTo> buffer, Func<TFrom, int, TTo> selector)
        {
            if(selector == null) { throw new ArgumentNullException(nameof(selector)); }
            if(source.Length > buffer.Length) { throw new ArgumentException($"Length of {nameof(buffer)} is too short."); }
            for(int i = 0; i < source.Length; i++) {
                buffer[i] = selector(source[i], i);
            }
            return buffer;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T First<T>(this Span<T> source, Func<T, bool> selector) => First((ReadOnlySpan<T>)source, selector);

        public static T First<T>(this ReadOnlySpan<T> source, Func<T, bool> selector)
        {
            if(selector == null) { throw new ArgumentNullException(nameof(selector)); }
            foreach(var item in source) {
                if(selector(item)) {
                    return item;
                }
            }
            throw new InvalidOperationException("No element matched.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FirstOrDefault<T>(this Span<T> source, Func<T, bool> selector) => FirstOrDefault((ReadOnlySpan<T>)source, selector);

        public static T FirstOrDefault<T>(this ReadOnlySpan<T> source, Func<T, bool> selector)
        {
            if(selector == null) { throw new ArgumentNullException(nameof(selector)); }
            foreach(var item in source) {
                if(selector(item)) {
                    return item;
                }
            }
            return default!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? FirstOrNull<T>(this Span<T> source, Func<T, bool> selector) where T : struct
            => FirstOrNull((ReadOnlySpan<T>)source, selector);

        public static T? FirstOrNull<T>(this ReadOnlySpan<T> source, Func<T, bool> selector) where T : struct
        {
            if(selector == null) { throw new ArgumentNullException(nameof(selector)); }
            foreach(var item in source) {
                if(selector(item)) {
                    return item;
                }
            }
            return null;
        }
    }
}
