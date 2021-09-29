#nullable enable
using System.Runtime.InteropServices;

namespace Elffy.Imaging
{
    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct TgaHeader
    {
        [FieldOffset(0)]
        internal readonly byte IDLength;

        [FieldOffset(1)]
        internal readonly bool HasColorMap;

        [FieldOffset(2)]
        internal readonly TgaDataFormat Format;

        [FieldOffset(3)]
        internal readonly ushort ColorMapIndex;

        [FieldOffset(5)]
        internal readonly ushort ColorMapLength;

        [FieldOffset(7)]
        internal readonly byte ColorMapSize;

        [FieldOffset(8)]
        internal readonly ushort OriginX;

        [FieldOffset(10)]
        internal readonly ushort OriginY;

        [FieldOffset(12)]
        internal readonly ushort Width;

        [FieldOffset(14)]
        internal readonly ushort Height;

        [FieldOffset(16)]
        internal readonly byte BitsPerPixel;

        [FieldOffset(17)]
        internal readonly byte Descriptor;


        internal readonly bool IsLeftToRight => (Descriptor & 0b00010000) != 0b00010000;

        internal readonly bool IsTopToBottom => (Descriptor & 0b00100000) == 0b00100000;
    }
}
