#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Effective.Unsafes;

namespace Elffy.Serialization.Fbx
{
    internal unsafe readonly struct NodeDictionary<T> : IDisposable where T : unmanaged
    {
        private const int TableCount = 1024;

        private readonly IntPtr _ptr;

        private Dic* Dict => (Dic*)_ptr;

        private NodeDictionary(Dic* dic)
        {
            _ptr = new IntPtr(dic);
        }

        public bool TryAdd(long key, in T value)
        {
            return Dict->TryAdd(key, value);
        }

        public void Dispose()
        {
            Dict->Dispose();
            Unsafe.AsRef(_ptr) = default;
        }

        public static NodeDictionary<T> New()
        {
            var dic = (Dic*)Marshal.AllocHGlobal(sizeof(Dic));
            return new NodeDictionary<T>(dic);
        }


        private struct Dic
        {
            private const int TableSize = 512;

            public readonly UnsafeRawArray<KeyValue> Tables;
            public readonly UnsafeRawList<UnsafeRawArray<KeyValue>> Bufs;
            private int bufPos;

            private Dic(int tableSize)
            {
                Tables = new UnsafeRawArray<KeyValue>(tableSize, true);
                Bufs = UnsafeRawList<UnsafeRawArray<KeyValue>>.New(0);
                bufPos = 0;
            }

            public bool TryAdd(long key, in T item)
            {
                var hash = key.GetHashCode() % TableSize;
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
                        if(t->Key == key) {
                            return false;
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


            public static Dic New()
            {
                return new Dic(TableSize);
            }

            public void Dispose()
            {
                Tables.Dispose();
                foreach(var buf in Bufs.AsSpan()) {
                    buf.Dispose();
                }
                Bufs.Dispose();
            }
        }

        private struct KeyValue
        {
            public long Key;
            public T Value;
            public KeyValue* Next;
            public bool HasValue;
        }
    }
}
