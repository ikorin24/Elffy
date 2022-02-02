#nullable enable
using System;
using System.Threading;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elffy.Effective.Unsafes;
using Elffy.Threading;

namespace Elffy.Effective
{
    internal static class ArrayMemoryPool
    {
        private const int ByteLenderCount = 3;
        private const int ObjLenderCount = 2;

        private const int ByteLender0Size = 256;
        private const int ByteLender1Size = 1024;
        private const int ByteLender2Size = 8192;

        private const int ObjLender0Size = 256;
        private const int ObjLender1Size = 1024;

        private static readonly Lazy<MemoryLender<byte>[]> _byteLenders = new(() => new MemoryLender<byte>[ByteLenderCount]
        {
            new(ByteLender0Size, 512),      // ~= 128 kB
            new(ByteLender1Size, 128),      // ~= 128 kB
            new(ByteLender2Size, 32),       // ~= 256 kB
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly Lazy<MemoryLender<object?>[]> _objLenders = new(() => new MemoryLender<object?>[ObjLenderCount]
        {
            new(ObjLender0Size, 256),       // ~= 512 kB (on 64bit)
            new(ObjLender1Size, 128),       // ~= 1024 kB (on 64bit)
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        public static unsafe bool TryRentValueTypeMemory<T>(int length, [MaybeNullWhen(false)] out byte[] array, out int start) where T : unmanaged
        {
            if(length > ByteLender2Size / sizeof(T) || length < 0) {
                array = null;
                start = 0;
                return false;
            }

            var byteLength = sizeof(T) * length;
            var lenders = _byteLenders.Value;
            Debug.Assert(lenders.Length == 3);
            var lender = lenders.At(0);
            if(byteLength <= lender.SegmentSize && lender.TryRent(out array, out start)) {
                return true;
            }
            lender = lenders.At(1);
            if(byteLength <= lender.SegmentSize && lender.TryRent(out array, out start)) {
                return true;
            }
            lender = lenders.At(2);
            if(byteLength <= lender.SegmentSize && lender.TryRent(out array, out start)) {
                return true;
            }
            array = null;
            start = 0;
            return false;
        }

        public static bool TryRentRefTypeMemory(int length, [MaybeNullWhen(false)] out object?[] array, out int start)
        {
            if(length > ObjLender1Size || length < 0) {
                array = null;
                start = 0;
                return false;
            }

            var lenders = _objLenders.Value;
            Debug.Assert(lenders.Length == 2);
            var lender = lenders.At(0);
            if(length <= lender.SegmentSize && lender.TryRent(out array, out start)) {
                return true;
            }
            lender = lenders.At(1);
            if(length <= lender.SegmentSize && lender.TryRent(out array, out start)) {
                return true;
            }
            array = null;
            start = 0;
            return false;
        }

        public static void ReturnValueTypeMemory(byte[] array, int start)
        {
            if(array == null) { return; }
            var lenders = _byteLenders.Value;
            Debug.Assert(lenders.Length == 3);
            var lender = lenders.At(0);
            if(lender.IsValidArray(array)) {
                lender.Return(start);
                return;
            }
            lender = lenders.At(1);
            if(lender.IsValidArray(array)) {
                lender.Return(start);
                return;
            }
            lender = lenders.At(2);
            if(lender.IsValidArray(array)) {
                lender.Return(start);
                return;
            }
            return;
        }

        public static void ReturnRefTypeMemory(object?[] array, int start)
        {
            if(array == null) { return; }
            var lenders = _objLenders.Value;
            Debug.Assert(lenders.Length == 2);

            var lender = lenders.At(0);
            if(lender.IsValidArray(array)) {
                lender.Return(start);
                return;
            }
            lender = lenders.At(1);
            if(lender.IsValidArray(array)) {
                lender.Return(start);
                return;
            }
            return;
        }

        private sealed class MemoryLender<T>
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
            private readonly BitArray _segmentState;        // True means the item is on loan.
            private readonly int[] _availableIDStack;
            private readonly int _maxCount;
            private readonly int _segmentSize;
            private int _availableHead;
            private FastSpinLock _sync;

            public int SegmentSize => _segmentSize;
            public int MaxCount => _maxCount;

            public int AvailableCount => _maxCount - _availableHead;

            public MemoryLender(int segmentSize, int segmentCount)
            {
                if(segmentSize <= 0) { throw new ArgumentOutOfRangeException(nameof(segmentSize)); }
                if(segmentCount <= 0) { throw new ArgumentOutOfRangeException(nameof(segmentCount)); }
                _segmentSize = segmentSize;

                if(RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                    _array = new T[segmentSize * segmentCount];
                }
                else {
#if NET5_0_OR_GREATER
                    _array = GC.AllocateUninitializedArray<T>(segmentSize * segmentCount, pinned: true);
#else
                    _array = new T[segmentSize * segmentCount];
#endif
                }
                _segmentState = new BitArray(segmentCount);
                _availableHead = 0;
                _maxCount = segmentCount;
                var availableIDStack = new int[segmentCount];
                for(int i = 0; i < availableIDStack.Length; i++) {
                    availableIDStack[i] = i;
                }
                _availableIDStack = availableIDStack;
            }

            public bool TryRent([MaybeNullWhen(false)] out T[] array, out int start)
            {
                _sync.Enter();          // --- begin sync
                var head = _availableHead;
                if(head >= _availableIDStack.Length) {
                    _sync.Exit();       // --- end sync
                    array = null;
                    start = 0;
                    return false;
                }
                else {
                    int segID;
                    try {
                        segID = _availableIDStack[head];
                        _availableHead = head + 1;
                        _segmentState[segID] = true;
                    }
                    finally {
                        _sync.Exit();   // --- end sync
                    }

                    array = _array;
                    start = segID * _segmentSize;
                    return true;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Return(int start)
            {
                var segID = start / _segmentSize;
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsValidArray(T[] array)
            {
                return ReferenceEquals(array, _array);
            }
        }
    }
}
