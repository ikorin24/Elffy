#nullable enable
using System;
using System.Threading;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Elffy.Effective
{
    internal static class MemoryPool
    {
        private static readonly Lazy<ByteMemoryLender[]> _lenders = new Lazy<ByteMemoryLender[]>(() => new[]
        {
            new ByteMemoryLender(segmentSize: 256, segmentCount: 512),     // ~= 128 kB
            new ByteMemoryLender(segmentSize: 1024, segmentCount: 128),    // ~= 128 kB
            new ByteMemoryLender(segmentSize: 8192, segmentCount: 32),     // ~= 256 kB
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly Lazy<ObjectMemoryLender[]> _objLenders = new Lazy<ObjectMemoryLender[]>(() => new[]
        {
            new ObjectMemoryLender(segmentSize: 256, segmentCount: 256),     // ~= 512 kB (on 64bit)
            new ObjectMemoryLender(segmentSize: 1024, segmentCount: 128),    // ~= 1024 kB (on 64bit)
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        public static unsafe bool TryRentByteMemory<T>(int length, out byte[]? array, out int start, out int id, out int lenderNum) where T : unmanaged
        {
            var byteLength = sizeof(T) * length;
            var lenders = _lenders.Value;
            for(int i = 0; i < lenders.Length; i++) {
                if(byteLength <= lenders[i].SegmentSize && lenders[i].TryRent(out array, out start, out id)) {
                    lenderNum = i;
                    return true;
                }
            }
            array = null;
            start = 0;
            lenderNum = -1;
            id = -1;
            return false;
        }

        public static bool TryRentObjectMemory(int length, out object[]? array, out int start, out int id, out int lenderNum)
        {
            var lenders = _objLenders.Value;
            for(int i = 0; i < lenders.Length; i++) {
                if(length <= lenders[i].SegmentSize && lenders[i].TryRent(out array, out start, out id)) {
                    lenderNum = i;
                    return true;
                }
            }
            array = null;
            start = 0;
            lenderNum = -1;
            id = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ReturnByteMemory(int lender, int id)
        {
            if(lender < 0) { return; }
            var lenders = _lenders.Value;
            lenders[lender].Return(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ReturnObjectMemory(int lender, int id)
        {
            if(lender < 0) { return; }
            var lenders = _objLenders.Value;
            lenders[lender].Return(id);
        }


        abstract class MemoryLender<T>
        {
            // [NOTE]
            // ex) SegmentSize = 3, MaxCount = 4
            //
            // ---------------------------------------------------------------------
            //    _array     | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 |
            //     segID     |     0     |     1     |     2     |      3      |
            // _segmentState |   true    |   false   |   false   |    false    |
            // 
            // _availableIDStack  | X | 2 | 1 | 3 |         (X is whatever)
            //                          ↑
            //      Push (Return) ← _availableHead → Pop (Rent)
            // ---------------------------------------------------------------------
            //
            //                  ↓   Rent   → segID = 2
            // 
            // ---------------------------------------------------------------------
            //    _array     | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 |
            //     segID     |     0     |     1     |     2     |      3      |
            // _segmentState |   true    |   false   |   true    |    false    |
            // 
            // _availableIDStack  | X | X | 1 | 3 |         (X is whatever)
            //                              ↑
            //                            _availableHead
            // ---------------------------------------------------------------------

            private readonly T[] _array;
            private readonly BitArray _segmentState;        // true は貸し出し中、false は貸出可能
            private readonly int[] _availableIDStack;
            private int _availableHead;

            private FastSyncLock _sync;

            public int SegmentSize { get; }
            public int MaxCount => _availableIDStack.Length;

            public int AvailableCount => _availableIDStack.Length - _availableHead;

            public bool IsArrayPinned =>
#if NET5_0_OR_GREATER
                !RuntimeHelpers.IsReferenceOrContainsReferences<T>();
#else
                false;
#endif

            protected MemoryLender(int segmentSize, int segmentCount)
            {
                if(segmentSize <= 0) { throw new ArgumentOutOfRangeException(nameof(segmentSize)); }
                if(segmentCount <= 0) { throw new ArgumentOutOfRangeException(nameof(segmentCount)); }
                SegmentSize = segmentSize;

                if(RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                    _array = new T[segmentSize * segmentCount];
                    Debug.Assert(IsArrayPinned == false);
                }
                else {
#if NET5_0_OR_GREATER
                    _array = GC.AllocateUninitializedArray<T>(segmentSize * segmentCount, pinned: true);
                    Debug.Assert(IsArrayPinned);
#else
                    _array = new T[segmentSize * segmentCount];
                    Debug.Assert(IsArrayPinned == false);
#endif
                }
                _segmentState = new BitArray(segmentCount);

                _availableHead = 0;
                _availableIDStack = new int[segmentCount];
                for(int i = 0; i < _availableIDStack.Length; i++) {
                    _availableIDStack[i] = i;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryRent(out T[]? array, out int start, out int segID)
            {
                _sync.Enter();          // --- begin sync
                var head = _availableHead;
                if(head >= _availableIDStack.Length) {
                    _sync.Exit();       // --- end sync

                    segID = -1;
                    array = null;
                    start = 0;
                    return false;
                }
                else {
                    try {
                        segID = _availableIDStack[head];
                        _availableHead = head + 1;
                        _segmentState[segID] = true;
                    }
                    finally {
                        _sync.Exit();   // --- end sync
                    }

                    array = _array;
                    start = segID * SegmentSize;
                    return true;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Return(int segID)
            {
                if((uint)segID >= (uint)MaxCount) { return; }
                _sync.Enter();          // --- begin sync
                try {
                    if(_segmentState[segID] == true) {
                        _availableHead--;
                        _availableIDStack[_availableHead] = segID;
                        _segmentState[segID] = false;
                    }
                }
                finally {
                    _sync.Exit();       // --- end sync
                }
            }
        }

        class ByteMemoryLender : MemoryLender<byte>
        {
            public ByteMemoryLender(int segmentSize, int segmentCount) : base(segmentSize, segmentCount)
            {
            }
        }

        class ObjectMemoryLender : MemoryLender<object>
        {
            public ObjectMemoryLender(int segmentSize, int segmentCount) : base(segmentSize, segmentCount)
            {
            }
        }
    }
}
