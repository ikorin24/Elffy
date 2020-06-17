#nullable enable
using System;
using System.Threading;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Elffy.Effective
{
    public static class MemoryPool
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool TryRentByteMemory<T>(int length, out byte[]? array, out int start, out int rentByteLength, out int id, out int lenderNum) where T : unmanaged
        {
            Debug.Assert(typeof(T).IsValueType);
            var byteLength = sizeof(T) * length;
            var lenders = _lenders.Value;
            for(int i = 0; i < lenders.Length; i++) {
                if(byteLength <= lenders[i].SegmentSize && lenders[i].TryRent(out array, out start, out var byteMemoryLength, out id)) {
                    Debug.Assert(byteLength <= byteMemoryLength);
                    lenderNum = i;
                    rentByteLength = byteLength;
                    return true;
                }
            }
            array = null;
            start = 0;
            rentByteLength = 0;
            lenderNum = -1;
            id = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryRentObjectMemory(int length, out Memory<object> memory, out int id, out int lenderNum)
        {
            var lenders = _objLenders.Value;
            for(int i = 0; i < lenders.Length; i++) {
                if(length <= lenders[i].SegmentSize && lenders[i].TryRent(out var array, out var start, out var rentMemoryLength, out id)) {
                    Debug.Assert(length <= rentMemoryLength);
                    memory = array.AsMemory(start, length);
                    lenderNum = i;
                    return true;
                }
            }
            memory = Memory<object>.Empty;
            lenderNum = -1;
            id = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void ReturnByteMemory(int lender, int id)
        {
            if(lender < 0) { return; }
            var lenders = _lenders.Value;
            lenders[lender].Return(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void ReturnObjectMemory(int lender, int id)
        {
            if(lender < 0) { return; }
            var lenders = _objLenders.Value;
            lenders[lender].Return(id);
        }


        //[StructLayout(LayoutKind.Sequential, Pack = 0)]
        //private struct FieldSizeHelper<T>
        //{
        //    private T _field;
        //    private byte _offset;

        //    /// <summary><see cref="T"/>型の変数のサイズを取得します</summary>
        //    /// <returns>バイト数</returns>
        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    public static int GetSize()
        //    {
        //        var a = default(FieldSizeHelper<T>);
        //        return Unsafe.ByteOffset(ref Unsafe.As<T, byte>(ref a._field), ref a._offset).ToInt32();
        //    }
        //}


        internal abstract class MemoryLender<T>
        {
            // [NOTE]
            // ex) SegmentSize = 3, MaxCount = 4
            //
            // ---------------------------------------------------------------------
            //    _array     | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 |
            //     segID     |     0     |     1     |     2     |      3      |
            // _segmentState |   true    |   false   |   false   |    false    |
            // 
            // _availableIDStack  | -1 | 2 | 1 | 3 |
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
            // _availableIDStack  | -1 | -1 | 1 | 3 |
            //                               ↑
            //                            _availableHead
            // ---------------------------------------------------------------------

            private readonly T[] _array;
            private readonly BitArray _segmentState;        // true は貸し出し中、false は貸出可能
            private readonly int[] _availableIDStack;
            private int _availableHead;

            private object SyncRoot => this;

            public int SegmentSize { get; }
            public int MaxCount => _availableIDStack.Length;

            public int AvailableCount => _availableIDStack.Length - _availableHead;

            protected MemoryLender(int segmentSize, int segmentCount)
            {
                if(segmentSize <= 0) { throw new ArgumentOutOfRangeException(nameof(segmentSize)); }
                if(segmentCount <= 0) { throw new ArgumentOutOfRangeException(nameof(segmentCount)); }
                SegmentSize = segmentSize;
                _array = new T[segmentSize * segmentCount];
                _segmentState = new BitArray(segmentCount);

                _availableHead = 0;
                _availableIDStack = new int[segmentCount];
                for(int i = 0; i < _availableIDStack.Length; i++) {
                    _availableIDStack[i] = i;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryRent(out T[]? array, out int start, out int length, out int segID)
            {
                lock(SyncRoot) {
                    if(_availableHead >= _availableIDStack.Length) {
                        segID = -1;
                        array = null;
                        start = 0;
                        length = 0;
                        return false;
                    }
                    segID = _availableIDStack[_availableHead];
                    _availableIDStack[_availableHead] = -1;
                    _availableHead++;
                    _segmentState[segID] = true;
                }
                array = _array;
                start = segID * SegmentSize;
                length = SegmentSize;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Return(int segID)
            {
                if(segID < 0) { return; }
                lock(SyncRoot) {
                    if(_segmentState[segID] == false) { return; }
                    _availableHead--;
                    _availableIDStack[_availableHead] = segID;
                    _segmentState[segID] = false;
                }
            }
        }

        internal class ByteMemoryLender : MemoryLender<byte>
        {
            public ByteMemoryLender(int segmentSize, int segmentCount) : base(segmentSize, segmentCount)
            {
            }
        }

        internal class ObjectMemoryLender : MemoryLender<object>
        {
            public ObjectMemoryLender(int segmentSize, int segmentCount) : base(segmentSize, segmentCount)
            {
            }
        }
    }


    ///// <summary>
    ///// <see cref="PooledMemory{T}"/> のメモリ返却忘れのための魔除けのお守り
    ///// </summary>
    //internal sealed class MemoryPoolAmulet
    //{
    //    public int Lender { get; private set; }
    //    public int ID { get; private set; }
    //    public bool IsEnabled { get; private set; }

    //    public MemoryPoolAmulet()
    //    {
    //        ID = -1;
    //        Lender = -1;
    //    }

    //    ~MemoryPoolAmulet()
    //    {
    //        if(IsEnabled) {
    //            MemoryPool.Return(Lender, ID);
    //        }
    //    }

    //    public void Enable(int id, int lender)
    //    {
    //        ID = id;
    //        Lender = lender;
    //        IsEnabled = true;
    //    }

    //    public void Disable()
    //    {

    //        IsEnabled = false;
    //    }
    //}
}
