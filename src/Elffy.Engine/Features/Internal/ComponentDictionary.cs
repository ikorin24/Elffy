#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Elffy.Components;
using Elffy.Effective;
using Elffy.Threading;

namespace Elffy.Features.Internal
{
    [DebuggerTypeProxy(typeof(ComponentDictionaryTypeProxy))]
    [DebuggerDisplay("Count = {Count}")]
    internal struct ComponentDictionary     // mutable object, be careful
    {
        private const int StartOfFreeList = -3;

        private ArraySegment<int> _buckets;
        private ArraySegment<Entry> _entries;
        private ulong _fastModMultiplier;
        private int _count;
        private int _freeList;
        private int _freeCount;
        private int _version;

        public readonly int Count => _count - _freeCount;

        public readonly KeyCollection Keys => new KeyCollection(this);

        public readonly ValueCollection Values => new ValueCollection(this);

        public void Clear()
        {
            unsafe {
                Clear<object?>(null, null, null);
            }
        }

        public unsafe void Clear<TState>(delegate*<TState, void> onCleared,
                                         delegate*<TState, IComponent, void> eachItemAction,
                                         TState state)
        {
            int count = _count;
            if(count > 0) {
                Debug.Assert(_buckets.Array != null, "_buckets should be non-null");
                Debug.Assert(_entries.Array != null, "_entries should be non-null");

                var buckets = _buckets;
                var entries = _entries;
                _buckets = default;
                _entries = default;
                _fastModMultiplier = 0;
                _count = 0;
                _freeList = 0;
                _freeCount = 0;
                _version = 0;

                if(onCleared != null) {
                    onCleared(state);
                }

                if(eachItemAction != null) {
                    var entriesSpan = entries.AsSpan();
                    for(int i = 0; i < count; i++) {
                        if(entriesSpan[i].next >= -1) {
                            eachItemAction(state, entriesSpan[i].value);
                        }
                    }
                }

                if(InnerArrayPool.IsRent(buckets)) {
                    ReturnRentArray(buckets, entries);
                }
                //else {
                //    entries.AsSpan().Clear();
                //    buckets.AsSpan().Clear();
                //}
            }
        }

        private static void ReturnRentArray(in ArraySegment<int> buckets, in ArraySegment<Entry> entries)
        {
#if DEBUG
            Debug.Assert(InnerArrayPool.IsRent(buckets));
            Debug.Assert(InnerArrayPool.IsRent(entries));
#endif
            buckets.AsSpan().Clear();
            entries.AsSpan().Clear();
            InnerArrayPool.Return11(buckets, entries);
        }

        public readonly bool ContainsKey(Type key) => Unsafe.IsNullRef(ref FindValue(key)) == false;

        public readonly bool ContainsValue(IComponent value)
        {
            var entries = _entries.AsSpan();
            if(value == null) {
                return false;
            }
            else {
                for(int i = 0; i < _count; i++) {
                    if(entries[i].next >= -1 && ReferenceEquals(entries[i].value, value)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public readonly ref IComponent GetValueReference(Type key) => ref FindValue(key);

        public bool Remove(Type key, [MaybeNullWhen(false)] out IComponent value)
        {
            if(key is null) {
                ThrowArgumentNull(nameof(key));
            }

            if(_buckets.Array != null) {
                Debug.Assert(_entries.Array != null, "entries should be non-null");
                uint collisionCount = 0;
                uint hashCode = (uint)key.GetHashCode();
                ref int bucket = ref GetBucket(hashCode);
                var entries = _entries.AsSpan();
                int last = -1;
                int i = bucket - 1; // Value in buckets is 1-based
                while(i >= 0) {
                    ref var entry = ref entries[i];

                    if(entry.hashCode == hashCode && entry.key == key) {
                        if(last < 0) {
                            bucket = entry.next + 1; // Value in buckets is 1-based
                        }
                        else {
                            entries[last].next = entry.next;
                        }

                        value = entry.value;

                        Debug.Assert((StartOfFreeList - _freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
                        entry.next = StartOfFreeList - _freeList;
                        entry.key = default!;
                        entry.value = default!;
                        _freeList = i;
                        _freeCount++;
                        return true;
                    }

                    last = i;
                    i = entry.next;

                    collisionCount++;
                    if(collisionCount > (uint)entries.Length) {
                        // The chain of entries forms a loop; which means a concurrent update has happened.
                        // Break out of the loop and throw, rather than looping forever.
                        ThrowInvalidOperation();
                    }
                }
            }

            value = default;
            return false;
        }

        public readonly bool TryGetValue(Type key, [MaybeNullWhen(false)] out IComponent value)
        {
            ref var valRef = ref FindValue(key);
            if(!Unsafe.IsNullRef(ref valRef)) {
                value = valRef;
                return true;
            }

            value = default;
            return false;
        }

        public bool AddOrReplace(Type key, IComponent value, [MaybeNullWhen(false)] out IComponent oldValue)
        {
            return TryInsert(key, value, out oldValue, InsertionMode.Replace);
        }

        public bool TryAdd(Type key, IComponent value) => TryInsert(key, value, out _, InsertionMode.Add);

        public readonly Enumerator GetEnumerator() => new Enumerator(this);

        private readonly void CopyTo(KeyValuePair<Type, IComponent>[] dest)
        {
            int count = _count;
            if(dest.Length < count) {
                ThrowArgument("The destination is too short to copy to.");
            }

            var entries = _entries.AsSpan();
            for(int i = 0; i < count; i++) {
                if(entries[i].next >= -1) {
                    dest[i] = new KeyValuePair<Type, IComponent>(entries[i].key, entries[i].value);
                }
            }
        }

        private readonly ref IComponent FindValue(Type key)
        {
            if(key is null) {
                ThrowArgumentNull(nameof(key));
            }

            ref Entry entry = ref Unsafe.NullRef<Entry>();
            if(_buckets.Array != null) {
                Debug.Assert(_entries.Array != null, "expected entries to be != null");
                uint hashCode = (uint)key.GetHashCode();
                int i = GetBucket(hashCode);
                var entries = _entries.AsSpan();
                uint collisionCount = 0;

                i--; // Value in _buckets is 1-based; subtract 1 from i. We do it here so it fuses with the following conditional.
                do {
                    if((uint)i >= (uint)entries.Length) {
                        goto ReturnNotFound;
                    }

                    entry = ref entries[i];
                    if(entry.hashCode == hashCode && entry.key == key) {
                        goto ReturnFound;
                    }

                    i = entry.next;

                    collisionCount++;
                } while(collisionCount <= (uint)entries.Length);

                // The chain of entries forms a loop; which means a concurrent update has happened.
                // Break out of the loop and throw, rather than looping forever.
                goto ConcurrentOperation;
            }

            goto ReturnNotFound;

        ConcurrentOperation:
            ThrowInvalidOperation();
        ReturnFound:
            ref IComponent value = ref entry.value;
        Return:
            return ref value;
        ReturnNotFound:
            value = ref Unsafe.NullRef<IComponent>();
            goto Return;
        }

        private int Initialize()
        {
            Debug.Assert(_buckets.Array == null);
            Debug.Assert(_entries.Array == null);

            const int size = 11;  // prime number
            if(InnerArrayPool.TryRent11(out var buckets, out var entries) == false) {
                buckets = new int[size];
                entries = new Entry[size];
            }

            Debug.Assert(buckets.Array != null && buckets.Count == size);

            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            _freeList = -1;

            if(IntPtr.Size == 8) {
                _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
            }

            _buckets = buckets;
            _entries = entries;

            return size;
        }

        private bool TryInsert(Type key, IComponent value, out IComponent? oldValue, InsertionMode mode)
        {
            // [mode == InsertionMode.Add]
            //      The key already exists -> return false, oldValue = <old value>, value is not set in the entry
            //      The key does not exist -> return true,  oldValue = null,        value is set in the entry
            //
            // [mode == InsertionMode.Replace]
            //      The key already exists -> return true,  oldValue = <old value>, value is set in the entry
            //      The key does not exist -> return false, oldValue = null,        value is set in the entry

            if(key is null) {
                ThrowArgumentNull(nameof(key));
            }
            if(value is null) {
                ThrowArgumentNull(nameof(value));
            }

            if(_buckets.Array == null) {
                Initialize();
            }
            Debug.Assert(_buckets.Array != null);

            var entries = _entries.AsSpan();
            Debug.Assert(_entries.Array != null, "expected entries to be non-null");

            uint hashCode = (uint)key.GetHashCode();

            uint collisionCount = 0;
            ref int bucket = ref GetBucket(hashCode);
            int i = bucket - 1; // Value in _buckets is 1-based

            while(true) {
                if((uint)i >= (uint)entries.Length) { break; }

                if(entries[i].hashCode == hashCode && entries[i].key == key) {
                    // Tha case that the key is already exists.
                    Debug.Assert(entries[i].value is not null);
                    oldValue = entries[i].value;
                    if(mode == InsertionMode.Replace) {
                        entries[i].value = value;
                        return true;
                    }
                    Debug.Assert(mode == InsertionMode.Add);
                    return false;
                }
                i = entries[i].next;
                collisionCount++;
                if(collisionCount > (uint)entries.Length) {
                    // The chain of entries forms a loop; which means a concurrent update has happened.
                    // Break out of the loop and throw, rather than looping forever.
                    ThrowInvalidOperation();
                }
            }

            int index;
            if(_freeCount > 0) {
                index = _freeList;
                Debug.Assert((StartOfFreeList - entries[_freeList].next) >= -1, "shouldn't overflow because `next` cannot underflow");
                _freeList = StartOfFreeList - entries[_freeList].next;
                _freeCount--;
            }
            else {
                int count = _count;
                if(count == entries.Length) {
                    Resize();
                    bucket = ref GetBucket(hashCode);
                }
                index = count;
                _count = count + 1;
                entries = _entries.AsSpan();
            }

            // The case that the key does not exist in the entries.

            ref var entry = ref entries[index];
            entry.hashCode = hashCode;
            entry.next = bucket - 1; // Value in _buckets is 1-based
            entry.key = key;
            entry.value = value;
            bucket = index + 1; // Value in _buckets is 1-based
            _version++;
            oldValue = null;
            if(mode == InsertionMode.Add) {
                return true;
            }
            else {
                Debug.Assert(mode == InsertionMode.Replace);
                return false;
            }
        }

        private void Resize()
        {
            var newSize = HashHelpers.ExpandPrime(_count);

            Debug.Assert(_entries.Array != null, "_entries should be non-null");
            Debug.Assert(newSize >= _entries.Count);

            var entries = new Entry[newSize];
            var buckets = new int[newSize];

            int count = _count;
            _entries.AsSpan(0, count).CopyTo(entries);

            if(InnerArrayPool.IsRent(_buckets)) {
                ReturnRentArray(_buckets, _entries);
            }

            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            _buckets = buckets;
            if(IntPtr.Size == 8) {
                _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
            }
            for(int i = 0; i < count; i++) {
                if(entries[i].next >= -1) {
                    ref int bucket = ref GetBucket(entries[i].hashCode);
                    entries[i].next = bucket - 1; // Value in _buckets is 1-based
                    bucket = i + 1;
                }
            }

            _entries = entries;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ref int GetBucket(uint hashCode)
        {
            var buckets = _buckets.AsSpan();
            if(IntPtr.Size == 8) {
                return ref buckets[(int)HashHelpers.FastMod(hashCode, (uint)buckets.Length, _fastModMultiplier)];
            }
            else {
                // 32 bits runtime
                return ref buckets[(int)(hashCode % (uint)buckets.Length)];
            }
        }

        private enum InsertionMode : byte
        {
            Add = 0,
            Replace = 1,
        }

        private struct Entry
        {
            public uint hashCode;
            /// <summary>
            /// 0-based index of next entry in chain: -1 means end of chain
            /// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
            /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
            /// </summary>
            public int next;
            public Type key;            // Key of entry
            public IComponent value;    // Value of entry
        }

        public struct Enumerator
        {
            private readonly ComponentDictionary _dictionary;
            private readonly int _version;
            private int _index;
            private KeyValuePair<Type, IComponent> _current;

            internal Enumerator(ComponentDictionary dictionary)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _index = 0;
                _current = default;
            }

            public bool MoveNext()
            {
                if(_version != _dictionary._version) {
                    ThrowInvalidOperation();
                }

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
                var entries = _dictionary._entries.AsSpan();
                while((uint)_index < (uint)_dictionary._count) {
                    ref Entry entry = ref entries[_index++];

                    if(entry.next >= -1) {
                        _current = new KeyValuePair<Type, IComponent>(entry.key, entry.value);
                        return true;
                    }
                }

                _index = _dictionary._count + 1;
                _current = default;
                return false;
            }

            public KeyValuePair<Type, IComponent> Current => _current;

            public void Dispose() { }
        }

        [DebuggerTypeProxy(typeof(KeyCollectionTypeProxy))]
        [DebuggerDisplay("Count = {Count}")]
        public readonly struct KeyCollection
        {
            private readonly ComponentDictionary _dictionary;

            public int Count => _dictionary.Count;

            internal KeyCollection(ComponentDictionary dictionary) => _dictionary = dictionary;

            public Enumerator GetEnumerator() => new Enumerator(_dictionary);

            public void CopyTo(Span<Type> span)
            {
                if(span.Length < _dictionary.Count) {
                    ThrowArgument("The span is too short to copy to.");
                }

                int count = _dictionary._count;
                var entries = _dictionary._entries.AsSpan();
                for(int i = 0; i < count; i++) {
                    if(entries[i].next >= -1) {
                        span[i] = entries[i].key;
                    }
                }
            }

            public struct Enumerator
            {
                private readonly ComponentDictionary _dictionary;
                private int _index;
                private readonly int _version;
                private Type? _currenType;

                internal Enumerator(ComponentDictionary dictionary)
                {
                    _dictionary = dictionary;
                    _version = dictionary._version;
                    _index = 0;
                    _currenType = default;
                }

                public void Dispose() { }

                public bool MoveNext()
                {
                    if(_version != _dictionary._version) {
                        ThrowInvalidOperation();
                    }

                    var entries = _dictionary._entries.AsSpan();
                    while((uint)_index < (uint)_dictionary._count) {
                        ref Entry entry = ref entries[_index++];

                        if(entry.next >= -1) {
                            _currenType = entry.key;
                            return true;
                        }
                    }

                    _index = _dictionary._count + 1;
                    _currenType = default;
                    return false;
                }

                public Type Current => _currenType!;
            }
        }

        [DebuggerTypeProxy(typeof(ValueCollectionTypeProxy))]
        [DebuggerDisplay("Count = {Count}")]
        public readonly struct ValueCollection
        {
            private readonly ComponentDictionary _dictionary;

            internal ValueCollection(ComponentDictionary dictionary) => _dictionary = dictionary;

            public Enumerator GetEnumerator() => new Enumerator(_dictionary);

            public void CopyTo(Span<IComponent> span)
            {
                if(span.Length < _dictionary.Count) {
                    ThrowArgument("The span is too short to copy to.");
                }

                int count = _dictionary._count;
                var entries = _dictionary._entries.AsSpan();
                for(int i = 0; i < count; i++) {
                    if(entries[i].next >= -1) {
                        span[i] = entries[i].value;
                    }
                }
            }

            public int Count => _dictionary.Count;

            public struct Enumerator
            {
                private readonly ComponentDictionary _dictionary;
                private int _index;
                private readonly int _version;
                private IComponent? _currenIComponent;

                internal Enumerator(ComponentDictionary dictionary)
                {
                    _dictionary = dictionary;
                    _version = dictionary._version;
                    _index = 0;
                    _currenIComponent = default;
                }

                public void Dispose() { }

                public bool MoveNext()
                {
                    if(_version != _dictionary._version) {
                        ThrowInvalidOperation();
                    }

                    var entries = _dictionary._entries.AsSpan();
                    while((uint)_index < (uint)_dictionary._count) {
                        ref Entry entry = ref entries[_index++];

                        if(entry.next >= -1) {
                            _currenIComponent = entry.value;
                            return true;
                        }
                    }
                    _index = _dictionary._count + 1;
                    _currenIComponent = default;
                    return false;
                }

                public IComponent Current => _currenIComponent!;
            }
        }

        [DoesNotReturn]
        private static void ThrowArgument(string message) => throw new ArgumentException(message);

        [DoesNotReturn]
        private static void ThrowArgumentNull(string message) => throw new ArgumentNullException(message);

        [DoesNotReturn]
        private static void ThrowInvalidOperation() => throw new InvalidOperationException();

        private static class InnerArrayPool
        {
            private const int SegmentSize = 11;
            private const int SegmentCount = 256;   // If change this value, change 'GetSegmentIndex' method
            private static readonly BitArray _segmentState;
            private static readonly int[] _availableStack;
            private static readonly int[] _array;
            private static readonly Entry[] _entryArray;
            private static int _next;
            private static FastSpinLock _lock;


            static InnerArrayPool()
            {
                var availableStack = new int[SegmentCount];
                var segmentState = new BitArray(SegmentCount);
                var array = new int[SegmentSize * SegmentCount];
                var entryArray = new Entry[SegmentSize * SegmentCount];
                for(int i = 0; i < availableStack.Length; i++) {
                    availableStack[i] = i;
                }
                _segmentState = segmentState;
                _availableStack = availableStack;
                _array = array;
                _entryArray = entryArray;
                _next = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int GetSegmentIndex(int arraySegmentOffset)
            {
                Debug.Assert(SegmentCount == 256);
                return (arraySegmentOffset >> 8);   // arraySegmentOffset / SegmentCount
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsRent(in ArraySegment<int> segment) => segment.Array == _array && segment.Count == SegmentSize;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsRent(in ArraySegment<Entry> segment) => segment.Array == _entryArray && segment.Count == SegmentSize;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryRent11(out ArraySegment<int> bucketsSegment, out ArraySegment<Entry> entriesSegment)
            {
                try {
                    _lock.Enter();
                    var next = _next;
                    if(next >= SegmentCount) {
                        bucketsSegment = default;
                        entriesSegment = default;
                        return false;
                    }
                    var segmentIndex = _availableStack[next];
                    _next = next + 1;
                    _segmentState[segmentIndex] = true;
                    var start = next * SegmentSize;
                    bucketsSegment = new ArraySegment<int>(_array, start, SegmentSize);
                    entriesSegment = new ArraySegment<Entry>(_entryArray, start, SegmentSize);
                    return true;
                }
                finally {
                    _lock.Exit();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Return11(in ArraySegment<int> segment, in ArraySegment<Entry> entriesSegment)
            {
                var segmentIndex = GetSegmentIndex(segment.Offset);

#if DEBUG
                Debug.Assert(IsRent(segment));
                Debug.Assert(IsRent(entriesSegment));
                Debug.Assert(segmentIndex == GetSegmentIndex(entriesSegment.Offset));
#endif
                try {
                    _lock.Enter();
                    if(_segmentState[segmentIndex]) {
                        _next--;
                        _availableStack[_next] = segmentIndex;
                        _segmentState[segmentIndex] = false;
                    }
                }
                finally {
                    _lock.Exit();
                }
            }
        }

        private static class HashHelpers
        {
            public const uint HashCollisionThreshold = 100;

            // This is the maximum prime smaller than Array.MaxLength.
            public const int MaxPrimeArrayLength = 0x7FFFFFC3;

            public const int HashPrime = 101;

            // Table of prime numbers to use as hash table sizes.
            // A typical resize algorithm would pick the smallest prime number in this array
            // that is larger than twice the previous capacity.
            // Suppose our Hashtable currently has capacity x and enough elements are added
            // such that a resize needs to occur. Resizing first computes 2x then finds the
            // first prime in the table greater than 2x, i.e. if primes are ordered
            // p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n.
            // Doubling is important for preserving the asymptotic complexity of the
            // hashtable operations such as add.  Having a prime guarantees that double
            // hashing does not lead to infinite loops.  IE, your hash function will be
            // h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
            // We prefer the low computation costs of higher prime numbers over the increased
            // memory allocation of a fixed prime number i.e. when right sizing a HashSet.
            private static readonly int[] s_primes =
            {
                3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
                1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
                17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
                187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
                1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
            };

            public static bool IsPrime(int candidate)
            {
                if((candidate & 1) != 0) {
                    int limit = (int)Math.Sqrt(candidate);
                    for(int divisor = 3; divisor <= limit; divisor += 2) {
                        if((candidate % divisor) == 0)
                            return false;
                    }
                    return true;
                }
                return candidate == 2;
            }

            public static int GetPrime(int min)
            {
                if(min < 0)
                    throw new ArgumentException();

                foreach(int prime in s_primes) {
                    if(prime >= min)
                        return prime;
                }

                // Outside of our predefined table. Compute the hard way.
                for(int i = (min | 1); i < int.MaxValue; i += 2) {
                    if(IsPrime(i) && ((i - 1) % HashPrime != 0))
                        return i;
                }
                return min;
            }

            // Returns size of hashtable to grow to.
            public static int ExpandPrime(int oldSize)
            {
                int newSize = 2 * oldSize;

                // Allow the hashtables to grow to maximum possible size (~2G elements) before encountering capacity overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize) {
                    Debug.Assert(MaxPrimeArrayLength == GetPrime(MaxPrimeArrayLength), "Invalid MaxPrimeArrayLength");
                    return MaxPrimeArrayLength;
                }

                return GetPrime(newSize);
            }

            /// <summary>Returns approximate reciprocal of the divisor: ceil(2**64 / divisor).</summary>
            /// <remarks>This should only be used on 64-bit.</remarks>
            public static ulong GetFastModMultiplier(uint divisor) => ulong.MaxValue / divisor + 1;

            /// <summary>Performs a mod operation using the multiplier pre-computed with <see cref="GetFastModMultiplier"/>.</summary>
            /// <remarks>This should only be used on 64-bit.</remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint FastMod(uint value, uint divisor, ulong multiplier)
            {
                // We use modified Daniel Lemire's fastmod algorithm (https://github.com/dotnet/runtime/pull/406),
                // which allows to avoid the long multiplication if the divisor is less than 2**31.
                Debug.Assert(divisor <= int.MaxValue);

                // This is equivalent of (uint)Math.BigMul(multiplier * value, divisor, out _). This version
                // is faster than BigMul currently because we only need the high bits.
                uint highbits = (uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32);

                Debug.Assert(highbits == value % divisor);
                return highbits;
            }
        }

        private sealed class ComponentDictionaryTypeProxy
        {
            private KeyValuePair<Type, IComponent>[] _items;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public KeyValuePair<Type, IComponent>[] Items => _items;

            public ComponentDictionaryTypeProxy(ComponentDictionary entity)
            {
                var items = new KeyValuePair<Type, IComponent>[entity.Count];
                entity.CopyTo(items);
                _items = items;
            }
        }

        private sealed class KeyCollectionTypeProxy
        {
            private KeyCollection _entity;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public Type[] Items
            {
                get
                {
                    var items = new Type[_entity.Count];
                    _entity.CopyTo(items);
                    return items;
                }
            }

            public KeyCollectionTypeProxy(KeyCollection entity) => _entity = entity;
        }

        private sealed class ValueCollectionTypeProxy
        {
            private ValueCollection _entity;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public IComponent[] Items
            {
                get
                {
                    var items = new IComponent[_entity.Count];
                    _entity.CopyTo(items);
                    return items;
                }
            }

            public ValueCollectionTypeProxy(ValueCollection entity) => _entity = entity;
        }
    }
}
