#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Imaging.Internal
{
    internal static class BitArrayOperation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBit(ReadOnlySpan<byte> array, int index)
        {
            return ((array[index >> 3] >> (7 - (index & 7))) & 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBitsValue(ReadOnlySpan<byte> array, int index, int bitCount)
        {
            return bitCount switch
            {
                32 => array[index << 2] | (array[(index << 2) + 1] << 8) | (array[(index << 2) + 2] << 16) | (array[(index << 2) + 3] << 24),
                8 => array[index],
                24 => array[index * 3] | (array[index * 3 + 1] << 8) | (array[index * 3 + 2] << 16),
                1 => (array[index >> 3] >> (7 - (index & 7))) & 1,            // (array[index / 8] >> ((7 - index % 8) * 1)) & 0b1
                2 => (array[index >> 2] >> ((3 - (index & 3)) << 1)) & 3,     // (array[index / 4] >> ((3 - index % 4) * 2)) & 0b11
                4 => (array[index >> 1] >> ((1 - (index & 1)) << 2)) & 15,    // (array[index / 2] >> ((1 - index % 2) * 4)) & 0b1111
                16 => array[index << 1] | (array[(index << 1) + 1] << 8),
                _ => 0,
            };
        }
    }
}
