using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Collections;

namespace Elffy.Effective.Unsafes
{
    //[DebuggerTypeProxy(typeof(IDictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public class UnmanagedDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : unmanaged where TValue : unmanaged
    {
        private int[]? _buckets;
        private Entry[]? _entries;
        private ulong _fastModMultiplier;
        private int _count;
        private int _freeList;
        private int _freeCount;
        private int _version;
        private KeyCollection? _keys;
        private ValueCollection? _values;
        private const int StartOfFreeList = -3;

        public UnmanagedDictionary() : this(0) { }

        public UnmanagedDictionary(int capacity)
        {
            if(capacity < 0) { ThrowHelper.ArgOutOfRange(nameof(capacity)); }
            if(capacity > 0) {
                Initialize(capacity);
            }
        }

        public UnmanagedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this((collection as ICollection<KeyValuePair<TKey, TValue>>)?.Count ?? 0)
        {
            if(collection == null) { ThrowHelper.NullArg(nameof(collection)); }
            AddRange(collection);
        }

        private void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            // It is likely that the passed-in dictionary is Dictionary<TKey,TValue>. When this is the case,
            // avoid the enumerator allocation and overhead by looping through the entries array directly.
            // We only do this when dictionary is Dictionary<TKey,TValue> and not a subclass, to maintain
            // back-compat with subclasses that may have overridden the enumerator behavior.
            if(collection.GetType() == typeof(UnmanagedDictionary<TKey, TValue>)) {
                UnmanagedDictionary<TKey, TValue> source = (UnmanagedDictionary<TKey, TValue>)collection;

                if(source.Count == 0) {
                    // Nothing to copy, all done
                    return;
                }

                // This is not currently a true .AddRange as it needs to be an initalized dictionary
                // of the correct size, and also an empty dictionary with no current entities (and no argument checks).
                Debug.Assert(source._entries is not null);
                Debug.Assert(_entries is not null);
                Debug.Assert(_entries.Length >= source.Count);
                Debug.Assert(_count == 0);

                Entry[] oldEntries = source._entries;
                CopyEntries(oldEntries, source._count);
                return;
            }

            // Fallback path for IEnumerable that isn't a non-subclassed Dictionary<TKey,TValue>.
            foreach(KeyValuePair<TKey, TValue> pair in collection) {
                Add(pair.Key, pair.Value);
            }
        }

        public int Count => _count - _freeCount;

        public KeyCollection Keys => _keys ??= new KeyCollection(this);


        public ValueCollection Values => _values ??= new ValueCollection(this);


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
                Debug.Assert(_buckets != null, "_buckets should be non-null");
                Debug.Assert(_entries != null, "_entries should be non-null");

                _buckets.AsSpan().Clear();

                _count = 0;
                _freeList = -1;
                _freeCount = 0;
                Array.Clear(_entries, 0, count);
            }
        }

        public bool ContainsKey(TKey key) => !UnsafeEx.IsNullRef(ref FindValue(key));

        public bool ContainsValue(TValue value)
        {
            Entry[]? entries = _entries;
            // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
            for(int i = 0; i < _count; i++) {
                if(entries![i].next >= -1 && EqualityComparer<TValue>.Default.Equals(entries[i].value, value)) {
                    return true;
                }
            }

            return false;
        }

        private void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if(array == null) { ThrowHelper.NullArg(nameof(array)); }
            if((uint)index > (uint)array.Length) { ThrowHelper.ArgOutOfRange(nameof(index)); }
            if(array.Length - index < Count) { ThrowHelper.Arg(nameof(array) + " is too short"); }

            int count = _count;
            Entry[]? entries = _entries;
            for(int i = 0; i < count; i++) {
                if(entries![i].next >= -1) {
                    array[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
                }
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        internal ref TValue FindValue(TKey key)
        {
            ref Entry entry = ref UnsafeEx.NullRef<Entry>();
            if(_buckets != null) {
                Debug.Assert(_entries != null, "expected entries to be != null");
                uint hashCode = (uint)key.GetHashCode();
                int i = GetBucket(hashCode);
                Entry[]? entries = _entries;
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
            int[] buckets = new int[size];
            Entry[] entries = new Entry[size];

            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            _freeList = -1;

            if(IntPtr.Size == 8) {
                _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
            }
            _buckets = buckets;
            _entries = entries;

            return size;
        }

        private bool TryInsert(TKey key, in TValue value, InsertionBehavior behavior)
        {
            if(_buckets == null) {
                Initialize(0);
            }
            Debug.Assert(_buckets != null);

            Entry[]? entries = _entries;
            Debug.Assert(entries != null, "expected entries to be non-null");

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
                entries = _entries;
            }

            ref Entry entry = ref entries![index];
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
            Debug.Assert(_entries != null, "_entries should be non-null");
            Debug.Assert(newSize >= _entries.Length);

            Entry[] entries = new Entry[newSize];

            int count = _count;
            Array.Copy(_entries, entries, count);

            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            _buckets = new int[newSize];
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

        public bool Remove(TKey key)
        {
            // The overload Remove(TKey key, out TValue value) is a copy of this method with one additional
            // statement to copy the value for entry being removed into the output parameter.
            // Code has been intentionally duplicated for performance reasons.

            if(_buckets != null) {
                Debug.Assert(_entries != null, "entries should be non-null");
                uint collisionCount = 0;
                uint hashCode = (uint)key.GetHashCode();
                ref int bucket = ref GetBucket(hashCode);
                Entry[]? entries = _entries;
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

                        if(RuntimeHelpers.IsReferenceOrContainsReferences<TKey>()) {
                            entry.key = default!;
                        }

                        if(RuntimeHelpers.IsReferenceOrContainsReferences<TValue>()) {
                            entry.value = default!;
                        }

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

            if(_buckets != null) {
                Debug.Assert(_entries != null, "entries should be non-null");
                uint collisionCount = 0;
                uint hashCode = (uint)key.GetHashCode();
                ref int bucket = ref GetBucket(hashCode);
                Entry[]? entries = _entries;
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

                        if(RuntimeHelpers.IsReferenceOrContainsReferences<TKey>()) {
                            entry.key = default!;
                        }

                        if(RuntimeHelpers.IsReferenceOrContainsReferences<TValue>()) {
                            entry.value = default!;
                        }

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

            int currentCapacity = _entries == null ? 0 : _entries.Length;
            if(currentCapacity >= capacity) {
                return currentCapacity;
            }

            _version++;

            if(_buckets == null) {
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
            Entry[]? oldEntries = _entries;
            int currentCapacity = oldEntries == null ? 0 : oldEntries.Length;
            if(newSize >= currentCapacity) {
                return;
            }

            int oldCount = _count;
            _version++;
            Initialize(newSize);

            Debug.Assert(oldEntries is not null);

            CopyEntries(oldEntries, oldCount);
        }

        private void CopyEntries(Entry[] entries, int count)
        {
            Debug.Assert(_entries is not null);

            Entry[] newEntries = _entries;
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
            int[] buckets = _buckets!;
            if(IntPtr.Size == 8) {
                return ref buckets[HashHelpers.FastMod(hashCode, (uint)buckets.Length, _fastModMultiplier)];
            }
            else {
                return ref buckets[hashCode % (uint)buckets.Length];
            }
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
            private readonly UnmanagedDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private int _index;
            private KeyValuePair<TKey, TValue> _current;
            private readonly int _getEnumeratorRetType;  // What should Enumerator.Current return?

            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;

            internal Enumerator(UnmanagedDictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
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
                while((uint)_index < (uint)_dictionary._count) {
                    ref Entry entry = ref _dictionary._entries![_index++];

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

        //[DebuggerTypeProxy(typeof(DictionaryKeyCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {Count}")]
        public sealed class KeyCollection : ICollection<TKey>, ICollection, IReadOnlyCollection<TKey>
        {
            private readonly UnmanagedDictionary<TKey, TValue> _dictionary;

            public KeyCollection(UnmanagedDictionary<TKey, TValue> dictionary)
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
                Entry[]? entries = _dictionary._entries;
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

            void ICollection.CopyTo(Array array, int index) => CopyTo((TKey[])array, index);

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

            public struct Enumerator : IEnumerator<TKey>, IEnumerator
            {
                private readonly UnmanagedDictionary<TKey, TValue> _dictionary;
                private int _index;
                private readonly int _version;
                private TKey? _currentKey;

                internal Enumerator(UnmanagedDictionary<TKey, TValue> dictionary)
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

                    while((uint)_index < (uint)_dictionary._count) {
                        ref Entry entry = ref _dictionary._entries![_index++];

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

        //[DebuggerTypeProxy(typeof(DictionaryValueCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {Count}")]
        public sealed class ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
        {
            private readonly UnmanagedDictionary<TKey, TValue> _dictionary;

            public ValueCollection(UnmanagedDictionary<TKey, TValue> dictionary)
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
                Entry[]? entries = _dictionary._entries;
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

            void ICollection.CopyTo(Array array, int index) => CopyTo((TValue[])array, index);

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

            public struct Enumerator : IEnumerator<TValue>, IEnumerator
            {
                private readonly UnmanagedDictionary<TKey, TValue> _dictionary;
                private int _index;
                private readonly int _version;
                private TValue? _currentValue;

                internal Enumerator(UnmanagedDictionary<TKey, TValue> dictionary)
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
                        ref Entry entry = ref _dictionary._entries![_index++];

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
    }

    internal enum InsertionBehavior : byte
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
