#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Imaging
{
    internal ref struct BufferSpanReader
    {
        private readonly Span<byte> _span;
        private int _pos;

        public int Position { readonly get => _pos; set => _pos = value; }

        public readonly int Length => _span.Length;

        public readonly Span<byte> Span => _span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferSpanReader(Span<byte> span)
        {
            _span = span;
            _pos = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> NextSafe(int count)
        {
            var span = _span.Slice(_pos, count);
            _pos += count;
            return span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte NextByteSafe()
        {
            return ref _span[_pos++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> Next(int count)
        {
            var span = _span.SliceUnsafe(_pos, count);
            _pos += count;
            return span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte NextByte()
        {
            return ref _span.At(_pos++);
        }
    }
}
