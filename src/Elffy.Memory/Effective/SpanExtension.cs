﻿#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnmanageUtility;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Effective
{
    public static class SpanExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Head<T>(this Span<T> source) => ref MemoryMarshal.GetReference(source);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T Head<T>(this ReadOnlySpan<T> source) => ref MemoryMarshal.GetReference(source);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T HeadOrNull<T>(this Span<T> source) => ref source.GetPinnableReference();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T HeadOrNull<T>(this ReadOnlySpan<T> source) => ref source.GetPinnableReference();
        
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
        public static ReadOnlySpan<T> AsReadOnly<T>(this Span<T> source) => source;

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
        public static PooledArray<T> ToPooledArray<T>(this Span<T> source) => new PooledArray<T>(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledArray<T> ToPooledArray<T>(this ReadOnlySpan<T> source) => new PooledArray<T>(source);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TTo[] SelectToArray<TFrom, TTo>(this Span<TFrom> source, Func<TFrom, TTo> selector)
            => SelectToArray((ReadOnlySpan<TFrom>)source, selector);

        public static TTo[] SelectToArray<TFrom, TTo>(this ReadOnlySpan<TFrom> source, Func<TFrom, TTo> selector)
        {
            if(selector is null) { throw new ArgumentNullException(nameof(selector)); }
            var array = new TTo[source.Length];
            for(int i = 0; i < array.Length; i++) {
                array[i] = selector(source[i]);
            }
            return array;
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
        public static PooledArray<TTo> SelectToPooledArray<TFrom, TTo>(this Span<TFrom> source, Func<TFrom, TTo> selector)
            => SelectToPooledArray((ReadOnlySpan<TFrom>)source, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledArray<TTo> SelectToPooledArray<TFrom, TTo>(this ReadOnlySpan<TFrom> source, Func<TFrom, TTo> selector)
        {
            var pooled = new PooledArray<TTo>(source.Length);
            try {
                source.SelectToSpan(pooled.AsSpan(), selector);
                return pooled;
            }
            catch(Exception) {
                pooled.Dispose();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledArray<TTo> SelectToPooledArray<TFrom, TTo>(this Span<TFrom> source, Func<TFrom, int, TTo> selector)
            => SelectToPooledArray((ReadOnlySpan<TFrom>)source, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledArray<TTo> SelectToPooledArray<TFrom, TTo>(this ReadOnlySpan<TFrom> source, Func<TFrom, int, TTo> selector)
        {
            var pooled = new PooledArray<TTo>(source.Length);
            try {
                source.SelectToSpan(pooled.AsSpan(), selector);
                return pooled;
            }
            catch(Exception) {
                pooled.Dispose();
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

        [return: MaybeNull]
        public static T FirstOrDefault<T>(this Span<T> source) => FirstOrDefault((ReadOnlySpan<T>)source);

        [return: MaybeNull]
        public static T FirstOrDefault<T>(this ReadOnlySpan<T> source) => source.Length > 0 ? source[0] : default;

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

        public static T Aggregate<T>(this Span<T> source, Func<T, T, T> func) => Aggregate((ReadOnlySpan<T>)source, func);

        public static T Aggregate<T>(this ReadOnlySpan<T> source, Func<T, T, T> func)
        {
            if(func is null) { throw new ArgumentNullException(nameof(func)); }
            if(source.Length == 0) { throw new InvalidOperationException("Sequence contains no elements."); }
            T accum = source[0];
            for(int i = 1; i < source.Length; i++) {
                func(accum, source[i]);
            }
            return accum;
        }

        public static TAccumulate Aggregate<TSource, TAccumulate>(this Span<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
            => Aggregate((ReadOnlySpan<TSource>)source, seed, func);

        public static TAccumulate Aggregate<TSource, TAccumulate>(this ReadOnlySpan<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
        {
            if(func is null) { throw new ArgumentNullException(nameof(func)); }
            var accum = seed;
            foreach(var item in source) {
                accum = func(accum, item);
            }
            return accum;
        }

        public static TResult Aggregate<TSource, TAccumulate, TResult>(this Span<TSource> source,
                                                                       TAccumulate seed,
                                                                       Func<TAccumulate, TSource, TAccumulate> func,
                                                                       Func<TAccumulate, TResult> resultSelector)
            => Aggregate((ReadOnlySpan<TSource>)source, seed, func, resultSelector);

        public static TResult Aggregate<TSource, TAccumulate, TResult>(this ReadOnlySpan<TSource> source, 
                                                                       TAccumulate seed, 
                                                                       Func<TAccumulate, TSource, TAccumulate> func, 
                                                                       Func<TAccumulate, TResult> resultSelector)
        {
            if(func is null) { throw new ArgumentNullException(nameof(func)); }
            if(resultSelector is null) { throw new ArgumentNullException(nameof(resultSelector)); }
            var accum = seed;
            foreach(var item in source) {
                accum = func(accum, item);
            }
            return resultSelector(accum);
        }

        public static Span<T> Replace<T>(this Span<T> source, T oldValue, T newValue)
        {
            var eq = EqualityComparer<T>.Default;
            for(int i = 0; i < source.Length; i++) {
                if(eq.Equals(source[i], oldValue)) {
                    source[i] = newValue;
                }
            }
            return source;
        }

        public static ReadOnlySpan<char> Replace(this ReadOnlySpan<char> source, char oldValue, char newValue, Span<char> destBuffer)
        {
            if(destBuffer.Length != source.Length) { throw new ArgumentException($"Length of {nameof(destBuffer)} is not same as {nameof(source)}"); }
            for(int i = 0; i < source.Length; i++) {
                destBuffer[i] = (source[i] == oldValue) ? newValue : source[i];
            }
            return destBuffer;
        }
    }
}