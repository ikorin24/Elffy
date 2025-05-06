#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Elffy.Effective.Unsafes;

namespace Elffy.Effective
{
    public static partial class SpanExtension
    {
        /// <summary>Get reference to 0th element of span</summary>
        /// <remarks>Don't call if empty span</remarks>
        /// <typeparam name="T">element type</typeparam>
        /// <param name="source">source span</param>
        /// <returns>reference to 0th element</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetReference<T>(this Span<T> source) => ref MemoryMarshal.GetReference(source);

        /// <summary>Get reference to 0th element of span</summary>
        /// <remarks>Don't call if empty span</remarks>
        /// <typeparam name="T">element type</typeparam>
        /// <param name="source">source span</param>
        /// <returns>reference to 0th element</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T GetReference<T>(this ReadOnlySpan<T> source) => ref MemoryMarshal.GetReference(source);

        /// <summary>Get reference to 0th element. Returns reference to null if source span is empty.</summary>
        /// <typeparam name="T">element type</typeparam>
        /// <param name="source">source span</param>
        /// <returns>reference to 0th element or reference to null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetReferenceOrNull<T>(this Span<T> source) => ref source.GetPinnableReference();

        /// <summary>Get reference to 0th element. Returns reference to null if source span is empty.</summary>
        /// <typeparam name="T">element type</typeparam>
        /// <param name="source">source span</param>
        /// <returns>reference to 0th element or reference to null</returns>        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T GetReferenceOrNull<T>(this ReadOnlySpan<T> source) => ref source.GetPinnableReference();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T* AsPointer<T>(this Span<T> source) where T : unmanaged
        {
            return (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T* AsPointer<T>(this ReadOnlySpan<T> source) where T : unmanaged
        {
            return (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T* AsPointerOrNull<T>(this Span<T> source) where T : unmanaged
        {
            return source.IsEmpty ? null : (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T* AsPointerOrNull<T>(this ReadOnlySpan<T> source) where T : unmanaged
        {
            return source.IsEmpty ? null : (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
        }

        /// <summary>Get byte span from source span</summary>
        /// <typeparam name="T">element type</typeparam>
        /// <param name="source">source span</param>
        /// <returns>byte span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> AsBytes<T>(this Span<T> source) where T : unmanaged => MemoryMarshal.AsBytes(source);

        /// <summary>Get byte span from source span</summary>
        /// <typeparam name="T">element type</typeparam>
        /// <param name="source">source span</param>
        /// <returns>byte span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> AsBytes<T>(this ReadOnlySpan<T> source) where T : unmanaged => MemoryMarshal.AsBytes(source);

        /// <summary>Get byte length of source span</summary>
        /// <typeparam name="T">element type</typeparam>
        /// <param name="source">source span</param>
        /// <returns>byte length</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetByteLength<T>(this Span<T> source) where T : unmanaged => Unsafe.SizeOf<T>() * source.Length;

        /// <summary>Get byte length of source span</summary>
        /// <typeparam name="T">element type</typeparam>
        /// <param name="source">source span</param>
        /// <returns>byte length</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetByteLength<T>(this ReadOnlySpan<T> source) where T : unmanaged => Unsafe.SizeOf<T>() * source.Length;

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
        public static Span<TTo> MarshalCast<TFrom, TTo>(this Span<TFrom> source) where TFrom : unmanaged
                                                                                 where TTo : unmanaged
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
        public static ReadOnlySpan<TTo> MarshalCast<TFrom, TTo>(this ReadOnlySpan<TFrom> source) where TFrom : unmanaged
                                                                                                 where TTo : unmanaged
        {
            return MemoryMarshal.Cast<TFrom, TTo>(source);
        }

        /// <summary>Convert <see cref="Span{T}"/> to <see cref="ReadOnlySpan{T}"/></summary>
        /// <typeparam name="T">element type</typeparam>
        /// <param name="source">source span</param>
        /// <returns><see cref="ReadOnlySpan{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnly<T>(this Span<T> source) => source;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeRawArray<T> ToRawArray<T>(this Span<T> source) where T : unmanaged
        {
            return new UnsafeRawArray<T>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeRawArray<T> ToRawArray<T>(this ReadOnlySpan<T> source) where T : unmanaged
        {
            return new UnsafeRawArray<T>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTypeRentMemory<T> ToValueTypeRentMemory<T>(this Span<T> source) where T : unmanaged
        {
            return ToValueTypeRentMemory((ReadOnlySpan<T>)source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTypeRentMemory<T> ToValueTypeRentMemory<T>(this ReadOnlySpan<T> source) where T : unmanaged
        {
            var dest = new ValueTypeRentMemory<T>(source.Length, false);
            source.CopyTo(dest.AsSpan());
            return dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RefTypeRentMemory<T> ToRefTypeRentMemory<T>(this Span<T> source) where T : class?
        {
            return ToRefTypeRentMemory((ReadOnlySpan<T>)source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RefTypeRentMemory<T> ToRefTypeRentMemory<T>(this ReadOnlySpan<T> source) where T : class?
        {
            var dest = new RefTypeRentMemory<T>(source.Length);
            source.CopyTo(dest.AsSpan());
            return dest;
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
            if(selector is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(selector));
            }
            var array = new TTo[source.Length];
            for(int i = 0; i < array.Length; i++) {
                array[i] = selector(source[i]);
            }
            return array;
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
            catch {
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
            catch {
                pooled.Dispose();
                throw;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<TTo> SelectToSpan<TFrom, TTo>(this Span<TFrom> source, Span<TTo> buffer, Func<TFrom, TTo> selector)
            => SelectToSpan((ReadOnlySpan<TFrom>)source, buffer, selector);

        public static Span<TTo> SelectToSpan<TFrom, TTo>(this ReadOnlySpan<TFrom> source, Span<TTo> buffer, Func<TFrom, TTo> selector)
        {
            if(selector == null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(selector));
            }
            if(source.Length > buffer.Length) {
                ThrowTooShort();
                [DoesNotReturn] static void ThrowTooShort() => throw new ArgumentException($"Length of {nameof(buffer)} is too short.");
            }
            for(int i = 0; i < source.Length; i++) {
                buffer.At(i) = selector(source[i]);
            }
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<TTo> SelectToSpan<TFrom, TTo>(this Span<TFrom> source, Span<TTo> buffer, Func<TFrom, int, TTo> selector)
            => SelectToSpan((ReadOnlySpan<TFrom>)source, buffer, selector);

        public static Span<TTo> SelectToSpan<TFrom, TTo>(this ReadOnlySpan<TFrom> source, Span<TTo> buffer, Func<TFrom, int, TTo> selector)
        {
            if(selector == null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(selector));
            }
            if(source.Length > buffer.Length) {
                ThrowTooShort();
                [DoesNotReturn] static void ThrowTooShort() => throw new ArgumentException($"Length of {nameof(buffer)} is too short.");
            }
            for(int i = 0; i < source.Length; i++) {
                buffer.At(i) = selector(source[i], i);
            }
            return buffer;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T First<T>(this Span<T> source, Func<T, bool> selector) => First((ReadOnlySpan<T>)source, selector);

        public static T First<T>(this ReadOnlySpan<T> source, Func<T, bool> selector)
        {
            if(selector == null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(selector));
            }
            foreach(var item in source) {
                if(selector(item)) {
                    return item;
                }
            }

            return ThrowNoElement();
            [DoesNotReturn] static T ThrowNoElement() => throw new InvalidOperationException("No element matched.");
        }

        [return: MaybeNull]
        public static T FirstOrDefault<T>(this Span<T> source) => FirstOrDefault((ReadOnlySpan<T>)source);

        [return: MaybeNull]
        public static T FirstOrDefault<T>(this ReadOnlySpan<T> source) => source.Length > 0 ? MemoryMarshal.GetReference(source) : default;

        [return: MaybeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FirstOrDefault<T>(this Span<T> source, Func<T, bool> selector) => FirstOrDefault((ReadOnlySpan<T>)source, selector);

        [return: MaybeNull]
        public static T FirstOrDefault<T>(this ReadOnlySpan<T> source, Func<T, bool> selector)
        {
            if(selector == null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(selector));
            }
            foreach(var item in source) {
                if(selector(item)) {
                    return item;
                }
            }
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? FirstOrNull<T>(this Span<T> source, Func<T, bool> selector) where T : struct
            => FirstOrNull((ReadOnlySpan<T>)source, selector);

        public static T? FirstOrNull<T>(this ReadOnlySpan<T> source, Func<T, bool> selector) where T : struct
        {
            if(selector == null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(selector));
            }
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
            if(func is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(func));
            }
            if(source.Length == 0) {
                ThrowArg();
                [DoesNotReturn] static void ThrowArg() => throw new ArgumentException("Sequence contains no elements.");
            }
            T accum = source.GetReference();
            for(int i = 1; i < source.Length; i++) {
                func(accum, source.At(i));
            }
            return accum;
        }

        public static TAccumulate Aggregate<TSource, TAccumulate>(this Span<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
            => Aggregate((ReadOnlySpan<TSource>)source, seed, func);

        public static TAccumulate Aggregate<TSource, TAccumulate>(this ReadOnlySpan<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
        {
            if(func is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(func));
            }
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
            if(func is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(func));
            }
            if(resultSelector is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(resultSelector));
            }
            var accum = seed;
            foreach(var item in source) {
                accum = func(accum, item);
            }
            return resultSelector(accum);
        }

#if !NET8_0_OR_GREATER
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
#endif

        public static ReadOnlySpan<char> Replace(this ReadOnlySpan<char> source, char oldValue, char newValue, Span<char> destBuffer)
        {
            if(destBuffer.Length < source.Length) {
                ThrowArg();
                [DoesNotReturn] static void ThrowArg() => throw new ArgumentException($"Length of {nameof(destBuffer)} is too short.");
            }
            for(int i = 0; i < source.Length; i++) {
                destBuffer.At(i) = (source[i] == oldValue) ? newValue : source[i];
            }
            return destBuffer.SliceUnsafe(0, source.Length);
        }
    }
}
