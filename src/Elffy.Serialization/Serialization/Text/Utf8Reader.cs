#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Effective.Unsafes;

namespace Elffy.Serialization.Text
{
    internal ref struct Utf8Reader
    {
        private readonly ReadOnlySpan<byte> _text;
        private int _pos;

        public ReadOnlySpan<byte> Text => _text;

        public ReadOnlySpan<byte> Current => _text.SliceUnsafe(_pos);

        public Utf8Reader(ReadOnlySpan<byte> text)
        {
            _text = text;
            _pos = 0;
        }

        public static implicit operator Utf8Reader(ReadOnlySpan<byte> text) => new Utf8Reader(text);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMatch(byte c) => (_pos < _text.Length) && (_text.At(_pos) == c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMatch(byte c1, byte c2) => (_pos + 1 < _text.Length) && (_text.At(_pos) == c1) && (_text.At(_pos + 1) == c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMatch(byte c1, byte c2, byte c3) => (_pos + 2 < _text.Length) && (_text.At(_pos) == c1) &&
                                                          (_text.At(_pos + 1) == c2) && (_text.At(_pos + 2) == c3);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMatch(byte c1, byte c2, byte c3, byte c4) => (_pos + 3 < _text.Length) && (_text.At(_pos) == c1) &&
                                                                   (_text.At(_pos + 1) == c2) && (_text.At(_pos + 2) == c3) &&
                                                                   (_text.At(_pos + 3) == c4);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMatch(ReadOnlySpan<byte> str) => _text.SliceUnsafe(_pos).StartsWith(str);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveIfMatch(byte c)
        {
            if(IsMatch(c)) {
                _pos += 1;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveIfMatch(byte c1, byte c2)
        {
            if(IsMatch(c1, c2)) {
                _pos += 2;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveIfMatch(byte c1, byte c2, byte c3)
        {
            if(IsMatch(c1, c2, c3)) {
                _pos += 3;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveIfMatch(byte c1, byte c2, byte c3, byte c4)
        {
            if(IsMatch(c1, c2, c3, c4)) {
                _pos += 4;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveIfMatch(ReadOnlySpan<byte> str)
        {
            if(IsMatch(str)) {
                _pos += str.Length;
                return true;
            }
            return false;
        }
    }
}
