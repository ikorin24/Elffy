#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Collections;
using Elffy.Effective.Unsafes;

namespace Elffy.Effective
{
    [DebuggerTypeProxy(typeof(BufferPooledDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public sealed class BufferPooledDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable
        where TKey : unmanaged
        where TValue : unmanaged
    {
        private ValueTypeRentMemory<int> _buckets;
        private ValueTypeRentMemory<Entry> _entries;
        private ulong _fastModMultiplier;
        private int _count;
        private int _freeList;
        private int _freeCount;
        private int _version;

        private const int StartOfFreeList = -3;

        public BufferPooledDictionary() : this(0) { }

        public BufferPooledDictionary(int capacity)
        {
            if(capacity < 0) { ThrowHelper.ArgOutOfRange(nameof(capacity)); }
            if(capacity > 0) {
                Initialize(capacity);
            }
        }

        public BufferPooledDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            : this((collection as ICollection<KeyValuePair<TKey, TValue>>)?.Count ?? 0)
        {
            if(collection is null) { ThrowHelper.NullArg(nameof(collection)); }
            AddRange(collection);
        }

        public BufferPooledDictionary(ReadOnlySpan<KeyValuePair<TKey, TValue>> span) : this(span.Length)
        {
            foreach(var (key, value) in span) {
                Add(key, value);
            }
        }


        private void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            // It is likely that the passed-in dictionary is Dictionary<TKey,TValue>. When this is the case,
            // avoid the enumerator allocation and overhead by looping through the entries array directly.
            // We only do this when dictionary is Dictionary<TKey,TValue> and not a subclass, to maintain
            // back-compat with subclasses that may have overridden the enumerator behavior.
            if(collection.GetType() == typeof(BufferPooledDictionary<TKey, TValue>)) {
                BufferPooledDictionary<TKey, TValue> source = (BufferPooledDictionary<TKey, TValue>)collection;

                if(source.Count == 0) {
                    // Nothing to copy, all done
                    return;
                }

                // This is not currently a true .AddRange as it needs to be an initalized dictionary
                // of the correct size, and also an empty dictionary with no current entities (and no argument checks).
                Debug.Assert(_entries.Length >= source.Count);
                Debug.Assert(_count == 0);

                var oldEntries = source._entries.AsSpan();
                CopyEntries(oldEntries, source._count);
                return;
            }

            // Fallback path for IEnumerable that isn't a non-subclassed Dictionary<TKey,TValue>.
            foreach(KeyValuePair<TKey, TValue> pair in collection) {
                Add(pair.Key, pair.Value);
            }
        }

        public int Count => _count - _freeCount;

        public KeyCollection Keys => new KeyCollection(this);


        public ValueCollection Values => new ValueCollection(this);


        public TValue this[TKey key]
        {
            get
            {
                ref TValue value = ref FindValue(key);
                if(!UnsafeEx.IsNullRef(ref value)) {
                    return value;
                }

                ThrowHelper.KeyNotFound(key);
                return default;
            }
            set
            {
                bool modified = TryInsert(key, value, InsertionBehavior.OverwriteExisting);
                Debug.Assert(modified);
            }
        }

        public void Add(TKey key, in TValue value)
        {
            bool modified = TryInsert(key, value, InsertionBehavior.ThrowOnExisting);
            Debug.Assert(modified); // If there was an existing key and the Add failed, an exception will already have been thrown.
        }

        public void Clear()
        {
            int count = _count;
            if(count > 0) {
                Debug.Assert(!_buckets.IsEmpty, "_buckets should be non-empty");
                Debug.Assert(!_entries.IsEmpty, "_entries should be non-empty");

                _buckets.AsSpan().Clear();

                _count = 0;
                _freeList = -1;
                _freeCount = 0;
                _entries.AsSpan(0, count).Clear();
            }
        }

        public bool ContainsKey(TKey key) => !UnsafeEx.IsNullRef(ref FindValue(key));

        public bool ContainsValue(TValue value)
        {
            var entries = _entries.AsSpan();
            // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
            for(int i = 0; i < _count; i++) {
                if(entries![i].next >= -1 && EqualityComparer<TValue>.Default.Equals(entries[i].value, value)) {
                    return true;
                }
            }

            return false;
        }

        internal void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if(array == null) { ThrowHelper.NullArg(nameof(array)); }
            if((uint)index > (uint)array.Length) { ThrowHelper.ArgOutOfRange(nameof(index)); }
            if(array.Length - index < Count) { ThrowHelper.Arg(nameof(array) + " is too short"); }

            int count = _count;
            var entries = _entries.AsSpan();
            for(int i = 0; i < count; i++) {
                if(entries![i].next >= -1) {
                    array[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
                }
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        private ref TValue FindValue(TKey key)
        {
            ref Entry entry = ref UnsafeEx.NullRef<Entry>();
            if(_buckets.IsEmpty == false) {
                uint hashCode = (uint)key.GetHashCode();
                int i = GetBucket(hashCode);
                var entries = _entries.AsSpan();
                uint collisionCount = 0;
                // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic

                i--; // Value in _buckets is 1-based; subtract 1 from i. We do it here so it fuses with the following conditional.
                do {
                    // Should be a while loop https://github.com/dotnet/runtime/issues/9422
                    // Test in if to drop range check for following array access
                    if((uint)i >= (uint)entries.Length) {
                        goto ReturnNotFound;
                    }

                    entry = ref entries[i];
                    if(entry.hashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entry.key, key)) {
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
            ThrowHelper.InvalidOperation_ConcurrentOperationsNotSupported();
        ReturnFound:
            ref TValue value = ref entry.value;
        Return:
            return ref value;
        ReturnNotFound:
            value = ref UnsafeEx.NullRef<TValue>();
            goto Return;
        }

        private int Initialize(int capacity)
        {
            int size = HashHelpers.GetPrime(capacity);
            var buckets = new ValueTypeRentMemory<int>(size, true);
            var entries = new ValueTypeRentMemory<Entry>(size, true);

            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            _freeList = -1;

            if(IntPtr.Size == 8) {
                _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
            }
            _buckets.Dispose();
            _buckets = buckets;
            _entries.Dispose();
            _entries = entries;

            return size;
        }

        private bool TryInsert(TKey key, in TValue value, InsertionBehavior behavior)
        {
            if(_buckets.IsEmpty) {
                Initialize(0);
            }
            Debug.Assert(!_buckets.IsEmpty);

            var entries = _entries.AsSpan();
            uint hashCode = (uint)key.GetHashCode();

            uint collisionCount = 0;
            ref int bucket = ref GetBucket(hashCode);
            int i = bucket - 1; // Value in _buckets is 1-based

            // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
            while(true) {
                // Should be a while loop https://github.com/dotnet/runtime/issues/9422
                // Test uint in if rather than loop condition to drop range check for following array access
                if((uint)i >= (uint)entries.Length) {
                    break;
                }

                if(entries[i].hashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entries[i].key, key)) {
                    if(behavior == InsertionBehavior.OverwriteExisting) {
                        entries[i].value = value;
                        return true;
                    }

                    if(behavior == InsertionBehavior.ThrowOnExisting) {
                        ThrowHelper.Arg_KeyDuplicated(key);
                    }

                    return false;
                }

                i = entries[i].next;

                collisionCount++;
                if(collisionCount > (uint)entries.Length) {
                    // The chain of entries forms a loop; which means a concurrent update has happened.
                    // Break out of the loop and throw, rather than looping forever.
                    ThrowHelper.InvalidOperation_ConcurrentOperationsNotSupported();
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

            ref Entry entry = ref entries[index];
            entry.hashCode = hashCode;
            entry.next = bucket - 1; // Value in _buckets is 1-based
            entry.key = key;
            entry.value = value;
            bucket = index + 1; // Value in _buckets is 1-based
            _version++;
            return true;
        }

        private void Resize() => Resize(HashHelpers.ExpandPrime(_count), false);

        private void Resize(int newSize, bool forceNewHashCodes)
        {
            // Value types never rehash
            Debug.Assert(!forceNewHashCodes || !typeof(TKey).IsValueType);
            Debug.Assert(newSize >= _entries.Length);

            var entries = new ValueTypeRentMemory<Entry>(newSize, false);

            int count = _count;
            _entries.AsSpan(0, count).CopyTo(entries.AsSpan());
            entries.AsSpan(count).Clear();

            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            var newBuckets = new ValueTypeRentMemory<int>(newSize, true);
            _buckets.Dispose();
            _buckets = newBuckets;

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

            _entries.Dispose();
            _entries = entries;
        }

        public bool Remove(TKey key)
        {
            // The overload Remove(TKey key, out TValue value) is a copy of this method with one additional
            // statement to copy the value for entry being removed into the output parameter.
            // Code has been intentionally duplicated for performance reasons.

            if(_buckets.IsEmpty == false) {
                uint collisionCount = 0;
                uint hashCode = (uint)key.GetHashCode();
                ref int bucket = ref GetBucket(hashCode);
                var entries = _entries.AsSpan();
                int last = -1;
                int i = bucket - 1; // Value in buckets is 1-based
                while(i >= 0) {
                    ref Entry entry = ref entries[i];

                    if(entry.hashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entry.key, key)) {
                        if(last < 0) {
                            bucket = entry.next + 1; // Value in buckets is 1-based
                        }
                        else {
                            entries[last].next = entry.next;
                        }

                        Debug.Assert((StartOfFreeList - _freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
                        entry.next = StartOfFreeList - _freeList;

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
                        ThrowHelper.InvalidOperation_ConcurrentOperationsNotSupported();
                    }
                }
            }
            return false;
        }

        public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            // This overload is a copy of the overload Remove(TKey key) with one additional
            // statement to copy the value for entry being removed into the output parameter.
            // Code has been intentionally duplicated for performance reasons.

            if(_buckets.IsEmpty == false) {
                uint collisionCount = 0;
                uint hashCode = (uint)key.GetHashCode();
                ref int bucket = ref GetBucket(hashCode);
                var entries = _entries.AsSpan();
                int last = -1;
                int i = bucket - 1; // Value in buckets is 1-based
                while(i >= 0) {
                    ref Entry entry = ref entries[i];

                    if(entry.hashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entry.key, key)) {
                        if(last < 0) {
                            bucket = entry.next + 1; // Value in buckets is 1-based
                        }
                        else {
                            entries[last].next = entry.next;
                        }

                        value = entry.value;

                        Debug.Assert((StartOfFreeList - _freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
                        entry.next = StartOfFreeList - _freeList;

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
                        ThrowHelper.InvalidOperation_ConcurrentOperationsNotSupported();
                    }
                }
            }

            value = default;
            return false;
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            ref TValue valRef = ref FindValue(key);
            if(!UnsafeEx.IsNullRef(ref valRef)) {
                value = valRef;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryAdd(TKey key, in TValue value) => TryInsert(key, value, InsertionBehavior.None);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        /// <summary>
        /// Ensures that the dictionary can hold up to 'capacity' entries without any further expansion of its backing storage
        /// </summary>
        public int EnsureCapacity(int capacity)
        {
            if(capacity < 0) {
                ThrowHelper.ArgOutOfRange(nameof(capacity));
            }

            int currentCapacity = _entries.Length;
            if(currentCapacity >= capacity) {
                return currentCapacity;
            }

            _version++;

            if(_buckets.IsEmpty) {
                return Initialize(capacity);
            }

            int newSize = HashHelpers.GetPrime(capacity);
            Resize(newSize, forceNewHashCodes: false);
            return newSize;
        }

        /// <summary>
        /// Sets the capacity of this dictionary to what it would be if it had been originally initialized with all its entries
        /// </summary>
        /// <remarks>
        /// This method can be used to minimize the memory overhead
        /// once it is known that no new elements will be added.
        ///
        /// To allocate minimum size storage array, execute the following statements:
        ///
        /// dictionary.Clear();
        /// dictionary.TrimExcess();
        /// </remarks>
        public void TrimExcess() => TrimExcess(Count);

        /// <summary>
        /// Sets the capacity of this dictionary to hold up 'capacity' entries without any further expansion of its backing storage
        /// </summary>
        /// <remarks>
        /// This method can be used to minimize the memory overhead
        /// once it is known that no new elements will be added.
        /// </remarks>
        public void TrimExcess(int capacity)
        {
            if(capacity < Count) {
                ThrowHelper.ArgOutOfRange(nameof(capacity));
            }

            int newSize = HashHelpers.GetPrime(capacity);
            var oldEntries = _entries.AsSpan();
            int currentCapacity = oldEntries.Length;
            if(newSize >= currentCapacity) {
                return;
            }

            int oldCount = _count;
            _version++;
            Initialize(newSize);
            CopyEntries(oldEntries, oldCount);
        }

        private void CopyEntries(ReadOnlySpan<Entry> entries, int count)
        {
            Debug.Assert(_entries.IsEmpty == false);

            var newEntries = _entries.AsSpan();
            int newCount = 0;
            for(int i = 0; i < count; i++) {
                uint hashCode = entries[i].hashCode;
                if(entries[i].next >= -1) {
                    ref Entry entry = ref newEntries[newCount];
                    entry = entries[i];
                    ref int bucket = ref GetBucket(hashCode);
                    entry.next = bucket - 1; // Value in _buckets is 1-based
                    bucket = newCount + 1;
                    newCount++;
                }
            }

            _count = newCount;
            _freeCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucket(uint hashCode)
        {
            var buckets = _buckets;
            if(IntPtr.Size == 8) {
                return ref buckets[(int)(HashHelpers.FastMod(hashCode, (uint)buckets.Length, _fastModMultiplier))];
            }
            else {
                return ref buckets[(int)(hashCode % (uint)buckets.Length)];
            }
        }

        ~BufferPooledDictionary() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            _buckets.Dispose();
            _entries.Dispose();
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
            public TKey key;     // Key of entry
            public TValue value; // Value of entry
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly BufferPooledDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private int _index;
            private KeyValuePair<TKey, TValue> _current;
            private readonly int _getEnumeratorRetType;  // What should Enumerator.Current return?

            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;

            internal Enumerator(BufferPooledDictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _index = 0;
                _getEnumeratorRetType = getEnumeratorRetType;
                _current = default;
            }

            public bool MoveNext()
            {
                if(_version != _dictionary._version) {
                    ThrowHelper.InvalidOperation_ModifiedOnEnumerating();
                }

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is int.MaxValue

                var entries = _dictionary._entries.AsSpan();
                while((uint)_index < (uint)_dictionary._count) {
                    ref Entry entry = ref entries[_index++];

                    if(entry.next >= -1) {
                        _current = new KeyValuePair<TKey, TValue>(entry.key, entry.value);
                        return true;
                    }
                }

                _index = _dictionary._count + 1;
                _current = default;
                return false;
            }

            public KeyValuePair<TKey, TValue> Current => _current;

            public void Dispose() { }

            object? IEnumerator.Current
            {
                get
                {
                    if(_index == 0 || (_index == _dictionary._count + 1)) {
                        ThrowHelper.InvalidOperation("");
                    }

                    if(_getEnumeratorRetType == DictEntry) {
                        return new DictionaryEntry(_current.Key, _current.Value);
                    }

                    return new KeyValuePair<TKey, TValue>(_current.Key, _current.Value);
                }
            }

            void IEnumerator.Reset()
            {
                if(_version != _dictionary._version) {
                    ThrowHelper.InvalidOperation_ModifiedOnEnumerating();
                }

                _index = 0;
                _current = default;
            }
        }

        [DebuggerTypeProxy(typeof(BufferPooledKeyCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {Count}")]
        public readonly struct KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>
        {
            private readonly BufferPooledDictionary<TKey, TValue> _dictionary;

            public KeyCollection(BufferPooledDictionary<TKey, TValue> dictionary)
            {
                if(dictionary == null) {
                    ThrowHelper.NullArg(nameof(dictionary));
                }

                _dictionary = dictionary;
            }

            public Enumerator GetEnumerator() => new Enumerator(_dictionary);

            public void CopyTo(TKey[] array, int index)
            {
                if(array == null) {
                    ThrowHelper.NullArg(nameof(array));
                }

                if(index < 0 || index > array.Length) {
                    ThrowHelper.ArgOutOfRange(nameof(index));
                }

                if(array.Length - index < _dictionary.Count) {
                    ThrowHelper.Arg(nameof(array) + " is too short");
                }

                int count = _dictionary._count;
                var entries = _dictionary._entries.AsSpan();
                for(int i = 0; i < count; i++) {
                    if(entries![i].next >= -1) array[index++] = entries[i].key;
                }
            }

            public int Count => _dictionary.Count;

            bool ICollection<TKey>.IsReadOnly => true;

            void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException();

            void ICollection<TKey>.Clear() => throw new NotSupportedException();

            bool ICollection<TKey>.Contains(TKey item) => _dictionary.ContainsKey(item);

            bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException();

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => new Enumerator(_dictionary);

            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_dictionary);

            public struct Enumerator : IEnumerator<TKey>, IEnumerator
            {
                private readonly BufferPooledDictionary<TKey, TValue> _dictionary;
                private int _index;
                private readonly int _version;
                private TKey? _currentKey;

                internal Enumerator(BufferPooledDictionary<TKey, TValue> dictionary)
                {
                    _dictionary = dictionary;
                    _version = dictionary._version;
                    _index = 0;
                    _currentKey = default;
                }

                public void Dispose() { }

                public bool MoveNext()
                {
                    if(_version != _dictionary._version) {
                        ThrowHelper.InvalidOperation_ModifiedOnEnumerating();
                    }

                    var entries = _dictionary._entries.AsSpan();
                    while((uint)_index < (uint)_dictionary._count) {
                        ref Entry entry = ref entries[_index++];

                        if(entry.next >= -1) {
                            _currentKey = entry.key;
                            return true;
                        }
                    }

                    _index = _dictionary._count + 1;
                    _currentKey = default;
                    return false;
                }

                public TKey Current => _currentKey!.Value;

                object? IEnumerator.Current
                {
                    get
                    {
                        if(_index == 0 || (_index == _dictionary._count + 1)) {
                            ThrowHelper.InvalidOperation("");
                        }

                        return _currentKey;
                    }
                }

                void IEnumerator.Reset()
                {
                    if(_version != _dictionary._version) {
                        ThrowHelper.InvalidOperation_ModifiedOnEnumerating();
                    }

                    _index = 0;
                    _currentKey = default;
                }
            }
        }

        [DebuggerTypeProxy(typeof(BufferPooledValueCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {Count}")]
        public readonly struct ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>
        {
            private readonly BufferPooledDictionary<TKey, TValue> _dictionary;

            public ValueCollection(BufferPooledDictionary<TKey, TValue> dictionary)
            {
                if(dictionary == null) {
                    ThrowHelper.NullArg(nameof(dictionary));
                }

                _dictionary = dictionary;
            }

            public Enumerator GetEnumerator() => new Enumerator(_dictionary);

            public void CopyTo(TValue[] array, int index)
            {
                if(array == null) {
                    ThrowHelper.NullArg(nameof(array));
                }

                if((uint)index > array.Length) {
                    ThrowHelper.ArgOutOfRange(nameof(index));
                }

                if(array.Length - index < _dictionary.Count) {
                    ThrowHelper.Arg(nameof(array) + " is too short");
                }

                int count = _dictionary._count;
                var entries = _dictionary._entries.AsSpan();
                for(int i = 0; i < count; i++) {
                    if(entries![i].next >= -1) array[index++] = entries[i].value;
                }
            }

            public int Count => _dictionary.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

            bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();

            void ICollection<TValue>.Clear() => throw new NotSupportedException();

            bool ICollection<TValue>.Contains(TValue item) => _dictionary.ContainsValue(item);

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => new Enumerator(_dictionary);

            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_dictionary);

            public struct Enumerator : IEnumerator<TValue>, IEnumerator
            {
                private readonly BufferPooledDictionary<TKey, TValue> _dictionary;
                private int _index;
                private readonly int _version;
                private TValue? _currentValue;

                internal Enumerator(BufferPooledDictionary<TKey, TValue> dictionary)
                {
                    _dictionary = dictionary;
                    _version = dictionary._version;
                    _index = 0;
                    _currentValue = default;
                }

                public void Dispose() { }

                public bool MoveNext()
                {
                    if(_version != _dictionary._version) {
                        ThrowHelper.InvalidOperation_ModifiedOnEnumerating();
                    }

                    while((uint)_index < (uint)_dictionary._count) {
                        ref Entry entry = ref _dictionary._entries[_index++];

                        if(entry.next >= -1) {
                            _currentValue = entry.value;
                            return true;
                        }
                    }
                    _index = _dictionary._count + 1;
                    _currentValue = default;
                    return false;
                }

                public TValue Current => _currentValue!.Value;

                object? IEnumerator.Current
                {
                    get
                    {
                        if(_index == 0 || (_index == _dictionary._count + 1)) {
                            ThrowHelper.InvalidOperation("");
                        }

                        return _currentValue;
                    }
                }

                void IEnumerator.Reset()
                {
                    if(_version != _dictionary._version) {
                        ThrowHelper.InvalidOperation_ModifiedOnEnumerating();
                    }

                    _index = 0;
                    _currentValue = default;
                }
            }
        }

        private enum InsertionBehavior : byte
        {
            /// <summary>
            /// The default insertion behavior.
            /// </summary>
            None = 0,

            /// <summary>
            /// Specifies that an existing entry with the same key should be overwritten if encountered.
            /// </summary>
            OverwriteExisting = 1,

            /// <summary>
            /// Specifies that if an existing entry with the same key is encountered, an exception should be thrown.
            /// </summary>
            ThrowOnExisting = 2
        }
    }

    internal sealed class BufferPooledDebugView<K, V> where K : unmanaged where V : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BufferPooledDictionary<K, V> _dict;

        internal BufferPooledDebugView(BufferPooledDictionary<K, V> dictionary)
        {
            if(dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            _dict = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<K, V>[] Items
        {
            get
            {
                KeyValuePair<K, V>[] items = new KeyValuePair<K, V>[_dict.Count];
                _dict.CopyTo(items, 0);
                return items;
            }
        }
    }

    internal sealed class BufferPooledKeyCollectionDebugView<TKey, TValue> where TKey : unmanaged where TValue : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BufferPooledDictionary<TKey, TValue>.KeyCollection _collection;

        internal BufferPooledKeyCollectionDebugView(BufferPooledDictionary<TKey, TValue>.KeyCollection collection)
        {
            _collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TKey[] Items
        {
            get
            {
                TKey[] items = new TKey[_collection.Count];
                _collection.CopyTo(items, 0);
                return items;
            }
        }
    }

    internal sealed class BufferPooledValueCollectionDebugView<TKey, TValue> where TKey : unmanaged where TValue : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BufferPooledDictionary<TKey, TValue>.ValueCollection _collection;

        internal BufferPooledValueCollectionDebugView(BufferPooledDictionary<TKey, TValue>.ValueCollection collection)
        {
            _collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TValue[] Items
        {
            get
            {
                TValue[] items = new TValue[_collection.Count];
                _collection.CopyTo(items, 0);
                return items;
            }
        }
    }
}
