#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Elffy
{
    [DebuggerDisplay("{ToString()}")]
    public readonly unsafe ref struct Utf8StringRef
    {
        private readonly IntPtr _ptr;   // byte*
        private readonly int _length;

        public int Lenght
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8StringRef(byte* ptr, int length)
        {
            if(length < 0) { ThrowOutOfRange(); }
            if(ptr == null && length != 0) { ThrowNullArg(); }

            _ptr = (IntPtr)ptr;
            _length = length;

            static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(length));
            static void ThrowNullArg() => throw new ArgumentNullException(nameof(ptr), nameof(ptr) + " is null but " + nameof(length) + " is not 0.");
        }

        public Utf8StringRef(byte* nullTerminatedStr)
        {
            _ptr = (IntPtr)nullTerminatedStr;
            if(nullTerminatedStr == null) {
                _length = 0;
            }
            else {
                var j = 0;
                for(j = 0; nullTerminatedStr[j] != 0; j++) { }
                _length = j;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<byte>(_ptr.ToPointer()), _length);
        }

        public override string ToString() => Encoding.UTF8.GetString(AsSpan());

        public override bool Equals(object? obj) => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(_ptr, _length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Utf8StringRef left, in Utf8StringRef right)
        {
            if(left._length != right._length) {
                return false;
            }
            else {
                if(left._ptr == right._ptr) {
                    return true;
                }
                else {
                    return left.AsSpan().SequenceEqual(right.AsSpan());
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Utf8StringRef left, in Utf8StringRef right) => !(left == right);
    }
}
