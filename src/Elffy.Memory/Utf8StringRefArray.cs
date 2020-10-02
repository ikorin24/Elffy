#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy
{
    [DebuggerDisplay("{DebugDisplay}")]
    public readonly unsafe ref struct Utf8StringRefArray
    {
        private readonly IntPtr _ptr;   // Utf8StringRef*
        private readonly int _length;

        private string DebugDisplay => $"{nameof(Utf8StringRef)}[{_length}]";

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        public ref readonly Utf8StringRef this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)index >= (uint)_length) { throw new ArgumentOutOfRangeException(nameof(index)); }
                return ref ((Utf8StringRef*)_ptr)[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8StringRefArray(Utf8StringRef* array, int length)
        {
            if(length < 0) { ThrowOutOfRange(); }
            if(array == null && length != 0) { ThrowNullArg(); }

            _ptr = (IntPtr)array;
            _length = length;

            static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(length));
            static void ThrowNullArg() => throw new ArgumentNullException(nameof(array), nameof(array) + " is null but " + nameof(length) + " is not 0.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(this);

        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => HashCode.Combine(_ptr, _length);

        public ref struct Enumerator
        {
            private readonly Utf8StringRefArray _array;
            private int _index;

            public Utf8StringRef Current { get; private set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(in Utf8StringRefArray array)
            {
                _array = array;
                _index = 0;
                Current = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if(_index < _array._length) {
                    Current = ((Utf8StringRef*)_array._ptr)[_index];
                    _index++;
                    return true;
                }
                else {
                    return false;
                }
            }

            public void Reset() => _index = 0;

            public void Dispose() { }
        }
    }
}
