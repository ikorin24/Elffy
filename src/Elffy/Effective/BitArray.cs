#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Effective
{
    public readonly struct BitArray : IEquatable<BitArray>
    {
        private readonly IntPtr[] _table;
        private readonly int _length;
        public readonly int Length => _length;

        public bool this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)index >= (uint)_length) { throw new ArgumentOutOfRangeException(nameof(index), index, "Out of range"); }
                var divided = Math.DivRem(index, IntPtr.Size * 8, out var mod);
                var mask = (IntPtr)(1 << mod);
                return And(_table[divided], mask) == mask;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if((uint)index >= (uint)_length) { throw new ArgumentOutOfRangeException(nameof(index), index, "Out of range"); }
                var divided = Math.DivRem(index, IntPtr.Size * 8, out var mod);
                var mask = (IntPtr)(1 << mod);
                if(value) {
                    _table[divided] = Or(_table[divided], mask);
                }
                else {
                    _table[divided] = And(_table[divided], Not(mask));
                }
            }
        }

        public BitArray(int length)
        {
            if(length < 0) { throw new ArgumentOutOfRangeException(nameof(length)); }
            if(length == 0) {
                _table = Array.Empty<IntPtr>();
            }
            else {
                var tableLen = 1 + (length - 1) / (IntPtr.Size * 8);
                _table = new IntPtr[tableLen];
            }
            _length = length;
        }

        public override bool Equals(object? obj) => obj is BitArray array && Equals(array);

        public bool Equals(BitArray other) => ReferenceEquals(_table, other._table) && _length == other._length;

        public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(_table), _length);

        public Enumerator GetEnumerator() => new Enumerator(this);


        private IntPtr Not(IntPtr value)
        {
            // JIT で分岐は消える
            return IntPtr.Size == 8 ? (IntPtr)(~(ulong)value) : (IntPtr)(~(uint)value);
        }

        private IntPtr And(IntPtr value1, IntPtr value2)
        {
            // JIT で分岐は消える
            return IntPtr.Size == 8 ? (IntPtr)((ulong)value1 & (ulong)value2) : (IntPtr)((uint)value1 & (uint)value2);
        }

        private IntPtr Or(IntPtr value1, IntPtr value2)
        {
            // JIT で分岐は消える
            return IntPtr.Size == 8 ? (IntPtr)((ulong)value1 | (ulong)value2) : (IntPtr)((uint)value1 | (uint)value2);
        }

        public struct Enumerator
        {
            private readonly BitArray _bitArray;
            private int _index;

            public Enumerator(BitArray bitArray)
            {
                _bitArray = bitArray;
                _index = -1;
            }

            public bool Current => _bitArray[_index];

            public void Dispose() { }

            public bool MoveNext()
            {
                return ++_index < _bitArray._length;
            }

            public void Reset()
            {
                _index = 0;
            }
        }
    }
}
