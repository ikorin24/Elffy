#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections;

namespace Elffy.Effective
{
    public static class MemoryPool
    {
        private static Lazy<MemoryLender[]> _lenders 
            = new Lazy<MemoryLender[]>(CreateLenders, LazyThreadSafetyMode.ExecutionAndPublication);

        private static MemoryLender[] Lenders => _lenders.Value;

        private static MemoryLender[] CreateLenders()
        {
            var lenders = new MemoryLender[]
            {
                // Sorted by segmentSize (small -> large)
                new MemoryLender(segmentSize: 128, count: 1024),    // ~= 128 kB
                new MemoryLender(segmentSize: 1024, count: 128),    // ~= 128 kB
                new MemoryLender(segmentSize: 8192, count: 32),     // ~= 256 kB
            };
            return lenders;
        }


        public static unsafe PooledMemory<T> GetMemory<T>(int length) where T : unmanaged
        {
            var byteLength = sizeof(T) * length;
            var lenders = Lenders;

            var rentMemory = Memory<byte>.Empty;
            var lenderNum = -1;
            var id = -1;
            for(int i = 0; i < lenders.Length; i++) {
                if(byteLength <= lenders[i].SegmentSize) {
                    rentMemory = lenders[i].Rent(out id);
                    lenderNum = i;
                }
            }

            var byteMemory = (id > 0) ? rentMemory.Slice(0, byteLength) : new byte[byteLength].AsMemory();

            return new PooledMemory<T>(byteMemory, id, lenderNum);
            
        }

        internal static unsafe void Return(int lender, int id)
        {
            var lenders = Lenders;
            lenders[lender].Return(id);
        }




        internal sealed class MemoryLender
        {
            private readonly byte[] _array;
            private readonly BitArray _segmentState;
            private readonly int[] _available;
            private int _next;

            public int SegmentSize { get; }
            public int MaxCount => _available.Length;

            private object _syncRoot => this;

            public MemoryLender(int segmentSize, int count)
            {
                if(segmentSize <= 0) { throw new ArgumentOutOfRangeException(nameof(segmentSize)); }
                if(count <= 0) { throw new ArgumentOutOfRangeException(nameof(count)); }
                SegmentSize = segmentSize;
                _array = new byte[segmentSize * count];
                _segmentState = new BitArray(count);

                _next = 0;
                _available = new int[count];
                for(int i = 0; i < _available.Length; i++) {
                    _available[i] = i;
                }
            }

            public Memory<byte> Rent(out int id)
            {
                lock(_syncRoot) {
                    if(_next >= _available.Length) {
                        id = -1;
                        return Memory<byte>.Empty;
                    }
                    id = _available[_next];
                    _available[_next] = -1;
                    _next++;
                    _segmentState[id] = true;
                }
                return _array.AsMemory(id * SegmentSize, SegmentSize);
            }

            public void Return(int id)
            {
                if(id < 0) { return; }
                lock(_syncRoot) {
                    if(_segmentState[id] == false) { return; }
                    _next--;
                    _available[_next] = id;
                    _segmentState[id] = false;
                }
            }
        }
    }

    public readonly struct PooledMemory<T> : IDisposable where T : unmanaged
    {
        // IMemoryOwner<T> を継承するメリットが特になく、
        // Memory<T> を公開する方法もないので
        // IMemoryOwner<T> は継承しない。

        private readonly Memory<byte> _byteMemory;
        private readonly int _id;
        private readonly int _lender;

        public readonly Span<T> Span => MemoryMarshal.Cast<byte, T>(_byteMemory.Span);

        internal PooledMemory(Memory<byte> byteMemory, int id, int lender)
        {
            _byteMemory = byteMemory;
            _id = id;
            _lender = lender;
        }

        public readonly void Dispose()
        {
            MemoryPool.Return(_lender, _id);
        }
    }
}
