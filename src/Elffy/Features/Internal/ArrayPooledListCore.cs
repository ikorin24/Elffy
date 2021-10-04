#nullable enable
using System;
using System.Numerics;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Elffy.Effective.Unsafes;
using Elffy.Mathematics;

namespace Elffy.Features.Internal
{
    internal struct ArrayPooledListCore<T>
    {
        private T[]? _array;
        private int _count;

        public int Count => _count;

        public int Capacity => _array?.Length ?? 0;     // Setter is not supported because capacity must be power of two.

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

        public void AddRange(ReadOnlySpan<T> items)
        {
            if(items.IsEmpty) { return; }
            EnsureCapacity(_count + items.Length);
            Debug.Assert(_array is not null);
            items.CopyTo(_array.AsSpan(_count));
            _count += items.Length;
        }

        public bool Remove(T item)
        {
            var span = AsSpan();
            var index = IndexOf(item);
            if(index < 0) {
                return false;
            }
            else {
                if(index != _count - 1) {
                    span.Slice(index + 1).CopyTo(span.Slice(index));
                }
                _count--;
                if(RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                    span[span.Length - 1] = default!;
                }
                return true;
            }
        }

        public void RemoveAt(int index)
        {
            if((uint)index >= (uint)_count) {
                ThrowOutOfRange();
                [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(index));
            }
            var span = AsSpan();
            if(index != _count - 1) {
                span.Slice(index + 1).CopyTo(span.Slice(index));
            }
            _count--;
            if(RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                span[span.Length - 1] = default!;
            }
        }

        public void Insert(int index, T item)
        {
            if((uint)index > (uint)_count) {
                ThrowOutOfRange();
                [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(index));
            }

            EnsureCapacity(_count + 1);
            Debug.Assert(_array is not null);
            Debug.Assert(_array.Length >= _count + 1);
            if(index != _count) {
                Debug.Assert(index >= 0 && index < _count);

                // 0 <= index < _count < _array.Length
                _array.AsSpan(index).CopyTo(_array.AsSpan(index + 1));
            }
            _array.At(index) = item;
            _count++;
        }

        public int IndexOf(T item)
        {
            var span = AsSpan();
            for(int i = 0; i < span.Length; i++) {
                if(EqualityComparer<T>.Default.Equals(span[i], item)) {
                    return i;
                }
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _count = 0;
            Pool.TryPush(ref _array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Clear(delegate*<T[]?, void> cleared)
        {
            _count = 0;
            Pool.TryPush(ref _array, cleared);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            // this method is valid if '_array' is null.
            return _array.AsSpan(0, _count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySliceEnumerator<T> GetEnumerator()
        {
            Debug.Assert((_array is null && _count == 0) || (_array is not null));     // count must be 0 when array is null.
            return new ArraySliceEnumerator<T>(_array, _count);
        }

        /// <summary>Release all pooled arrays in the current thread.</summary>
        /// <remarks>This method can increase the time it takes for the next GC.</remarks>
        public static void ReleasePool()
        {
            Pool.ReleasePool();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int capacity)
        {
            if(_array is null || _array.Length < capacity) {
                uint newCapacity = Math.Max(4, MathTool.RoundUpToPowerOfTwo((uint)capacity));
                Growth(newCapacity, ref this);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
            static void Growth(uint newCapacity, ref ArrayPooledListCore<T> instance)
            {
                Debug.Assert(newCapacity > instance._count);
                var current = instance._array;
                if(!Pool.TryGet(newCapacity, out instance._array)) {
                    if(RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                        instance._array = new T[newCapacity];
                    }
                    else {
                        instance._array = GC.AllocateUninitializedArray<T>((int)newCapacity);
                    }
                }
                Debug.Assert(instance._array.Length == newCapacity);

                if(instance._count > 0) {
                    Debug.Assert(current is not null);

                    // No index checking because
                    // instance._count < newCapacity == instance._array.Length
                    current.AsSpanUnsafe(0, instance._count).CopyTo(instance._array.AsSpanUnsafe());

                    Pool.TryPush(ref current);
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
                Debug.Assert(MathTool.IsPowerOfTwo(size), $"{nameof(size)} must be power of two.");

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
            public static unsafe bool TryPush(ref T[]? array) => TryPush(ref array, null);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe bool TryPush(ref T[]? array, delegate*<T[]?, void> cleared)
            {
                Debug.Assert(array is null || MathTool.IsPowerOfTwo(array.Length), $"Length of array must be power of two.");

                // 1. array = null
                // 2. Fire the delegete
                // 3. Clear elements in the array. (If needed)
                // 4. Pool the instance
                // ----------------------------------------------


                // 1. array = null
                var copy = array;
                array = null;

                // 2. Fire the delegete
                if(cleared != null) {
                    cleared(copy);
                }

                if(copy is null) {
                    return false;
                }

                // 3. Clear elements in the array. (If needed)
                if(RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                    Array.Clear(copy, 0, copy.Length);
                }

                // 4. Pool the instance
                var index = GetBucketIndex((uint)copy.Length);
                if((uint)index >= (uint)BucketCount) {
                    return false;
                }
                _buckets ??= new Bucket[BucketCount];
                ref var bucket = ref _buckets.At(index);
                if(bucket.IsEmpty) {
                    InitBucket(index, out bucket);
                }

                return bucket.TryPush(copy);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int GetBucketIndex(uint size)
            {
                return BitOperations.Log2(size) - 2;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
            private static void InitBucket(int index, out Bucket bucket)
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

            /// <summary>Release all pooled arrays in the current thread.</summary>
            /// <remarks>This method can increase the time it takes for the next GC.</remarks>
            public static void ReleasePool()
            {
                _buckets = null;
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

        [DebuggerDisplay("{Array}")]
        private struct ArrayWrap
        {
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[]? Array;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ArrayWrap(T[] array) => Array = array;
        }
    }
}
