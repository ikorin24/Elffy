#nullable enable
using System;
using System.Numerics;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Elffy.Effective.Unsafes;
using Elffy.Mathematics;

namespace Elffy.Core
{
    internal struct ArrayPooledListCore<T>
    {
        private T[]? _array;
        private int _count;

        public int Count => _count;

        public int Capacity => _array?.Length ?? 0;

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)index >= (uint)_count) {
                    ThrowOutOfRange();
                    [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(index));
                }
                Debug.Assert(_array is not null);
                return ref _array.At(index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            EnsureCapacity(_count + 1);
            Debug.Assert(_array is not null);
            _array.At(_count) = item;
            _count++;
        }

        public bool Remove(T item)
        {
            var span = AsSpan();
            for(int i = 0; i < span.Length; i++) {
                if(EqualityComparer<T>.Default.Equals(span[i], item)) {
                    if(i != _count - 1) {
                        span.Slice(i + 1).CopyTo(span.Slice(i));
                    }
                    _count--;
                    if(RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                        span[span.Length - 1] = default!;
                    }
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _count = 0;
            Pool.TryPush(ref _array);
            _array = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            // this method is valid if '_array' is null.
            return _array.AsSpan(0, _count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int capacity)
        {
            if(_array is null || _array.Length < capacity) {
                uint newCapacity = MathTool.RoundUpToPowerOfTwo((uint)capacity);
                Growth(newCapacity, out _array);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
            static void Growth(uint newCapacity, [NotNull] out T[]? array)
            {
                if(!Pool.TryGet(newCapacity, out array)) {
                    array = GC.AllocateUninitializedArray<T>((int)newCapacity);
                }
            }
        }

        private static class Pool
        {
            private const int BucketCount = 10;     //   Length  =  4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048
                                                    // PoolCount = |  1024  |     32     |          4          |
            private const int PoolCount1 = 1024;
            private const int PoolCount2 = 32;
            private const int PoolCount3 = 4;

            [ThreadStatic]
            private static Bucket[]? _buckets;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryGet(uint size, [MaybeNullWhen(false)] out T[] array)
            {
                Debug.Assert((size & (size - 1u)) == 0u, $"{nameof(size)} must be power of two.");

                var index = GetBucketIndex(size);
                if((uint)index >= (uint)BucketCount) {
                    array = null;
                    return false;
                }

                _buckets ??= new Bucket[BucketCount];
                ref var bucket = ref _buckets.At(index);
                if(bucket.IsEmpty) {
                    InitBucket(index, out bucket);
                }
                return bucket.TryGet(out array);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryPush(ref T[]? array)
            {
                if(array is null) {
                    return false;
                }
                Debug.Assert((array.Length & (array.Length - 1u)) == 0u, $"Length of array must be power of two.");

                if(RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                    Array.Clear(array, 0, array.Length);
                }

                var index = GetBucketIndex((uint)array.Length);
                if((uint)index >= (uint)BucketCount) {
                    array = null;
                    return false;
                }
                _buckets ??= new Bucket[BucketCount];
                ref var bucket = ref _buckets.At(index);
                if(bucket.IsEmpty) {
                    InitBucket(index, out bucket);
                }

                if(bucket.TryPush(array)) {
                    array = null;
                    return true;
                }
                else {
                    return false;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int GetBucketIndex(uint size)
            {
                return BitOperations.Log2(size) - 2;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
            static void InitBucket(int index, out Bucket bucket)
            {
                if(index <= 16) {
                    bucket = new Bucket(PoolCount1);
                }
                else if(index <= 128) {
                    bucket = new Bucket(PoolCount2);
                }
                else {
                    bucket = new Bucket(PoolCount3);
                }
            }
        }

        private struct Bucket
        {
            public int Count;
            private ArrayWrap[] _arrays;
            public bool IsEmpty => _arrays is null;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Bucket(int maxCount)
            {
                _arrays = new ArrayWrap[maxCount];
                Count = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryPush(T[] array)
            {
                if((uint)Count >= (uint)_arrays.Length) {
                    return false;
                }
                _arrays.At(Count) = new ArrayWrap(array);
                Count++;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGet([MaybeNullWhen(false)] out T[] array)
            {
                if(Count == 0) {
                    array = null;
                    return false;
                }
                Count--;
                ref var a = ref _arrays.At(Count);
                Debug.Assert(a.Array is not null);
                array = a.Array;
                a = default;
                return true;
            }
        }

        private struct ArrayWrap
        {
            public T[]? Array;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ArrayWrap(T[] array) => Array = array;
        }
    }
}
