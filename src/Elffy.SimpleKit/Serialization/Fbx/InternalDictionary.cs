#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Effective.Unsafes;

namespace Elffy.Serialization.Fbx
{
    internal unsafe readonly struct InternalDictionary<TKey, TValue> : IDisposable
        where TKey : unmanaged
        where TValue : unmanaged
    {
        private readonly IntPtr _ptr;   // Dic*

        private Dic* Dict => (Dic*)_ptr;

        private InternalDictionary(Dic* dic)
        {
            _ptr = new IntPtr(dic);
        }

        public void Add(TKey key, in TValue value)
        {
            if(Dict->TryAdd(key, value) == false) {
                ThrowKeyAlreadyExists(key);
            }

            static void ThrowKeyAlreadyExists(in TKey key) => throw new ArgumentException($"An item with the same key has already been added. Key: {key}");
        }

        public bool TryAdd(TKey key, in TValue value)
        {
            return Dict->TryAdd(key, value);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if(Dict->TryGet(key, out var p)) {
                value = *p;
                return true;
            }
            else {
                value = default;
                return false;
            }
        }

        public void Dispose()
        {
            if(_ptr != IntPtr.Zero) {
                Dic.Free(Dict);
                Unsafe.AsRef(_ptr) = IntPtr.Zero;
            }
        }

        public static InternalDictionary<TKey, TValue> New()
        {
            var dic = Dic.New();
            return new InternalDictionary<TKey, TValue>(dic);
        }


        private struct Dic
        {
            private const int DefaultTableSize = 512;

            public readonly UnsafeRawArray<KeyValue> Tables;
            public readonly UnsafeRawList<UnsafeRawArray<KeyValue>> Bufs;
            private int bufPos;

            private Dic(int tableSize)
            {
                Tables = new UnsafeRawArray<KeyValue>(tableSize, true);
                Bufs = UnsafeRawList<UnsafeRawArray<KeyValue>>.New(0);
                bufPos = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGet(TKey key, out TValue* value)
            {
                var hash = Hash(key);
                var table = Tables.GetPtr() + hash;
                if(table->HasValue == false) {
                    value = null;
                    return false;
                }
                if(EqualityComparer<TKey>.Default.Equals(table->Key, key)) {
                    value = &table->Value;
                    return true;
                }
                return Search(table->Next, key, out value);

                static bool Search(KeyValue* target, TKey key, out TValue* value)
                {
                    while(true) {
                        if(target == null) {
                            value = null;
                            return false;
                        }
                        if(EqualityComparer<TKey>.Default.Equals(target->Key, key)) {
                            value = &target->Value;
                            return true;
                        }
                        target = target->Next;
                    }
                }
            }

            public bool TryAdd(TKey key, in TValue item)
            {
                var hash = Hash(key);
                var table = Tables.GetPtr() + hash;
                if(table->HasValue == false) {
                    *table = new KeyValue()
                    {
                        Key = key,
                        Value = item,
                        HasValue = true,
                        Next = null,
                    };
                    return true;
                }
                else {
                    var t = table;
                    while(true) {
                        if(EqualityComparer<TKey>.Default.Equals(t->Key, key)) {
                            return false;       // The key already exists
                        }
                        if(t->Next == null) { break; }
                        t = t->Next;
                    }
                    var newKeyValue = NewKeyValueMem();
                    *newKeyValue = new KeyValue()
                    {
                        Key = key,
                        Value = item,
                        HasValue = true,
                        Next = null,
                    };
                    t->Next = newKeyValue;
                    return true;
                }
            }

            private int Hash(TKey key)
            {
                return key.GetHashCode() % DefaultTableSize;
            }

            private KeyValue* NewKeyValueMem()
            {
                if(Bufs.Count == 0) {
                    Debug.Assert(bufPos == 0);
                    var buf = new UnsafeRawArray<KeyValue>(1 << (Bufs.Count + 8), true);
                    Bufs.Add(buf);
                    bufPos = 1;
                    return buf.GetPtr();
                }
                else {
                    var buf = Bufs[Bufs.Count - 1];
                    if(buf.Length <= bufPos) {
                        buf = new UnsafeRawArray<KeyValue>(1 << (Bufs.Count + 8), true);
                        Bufs.Add(buf);
                        bufPos = 1;
                        return buf.GetPtr();
                    }
                    else {
                        var p = buf.GetPtr() + bufPos;
                        bufPos++;
                        return p;
                    }
                }
            }


            public static Dic* New()
            {
                var dic = (Dic*)Marshal.AllocHGlobal(sizeof(Dic));
                *dic = new Dic(DefaultTableSize);
                return dic;
            }

            public static void Free(Dic* dic)
            {
                dic->Tables.Dispose();
                foreach(var buf in dic->Bufs.AsSpan()) {
                    buf.Dispose();
                }
                dic->Bufs.Dispose();
                Marshal.FreeHGlobal(new IntPtr(dic));
            }
        }

        private struct KeyValue
        {
            public TKey Key;
            public TValue Value;
            public KeyValue* Next;
            public bool HasValue;
        }
    }
}
