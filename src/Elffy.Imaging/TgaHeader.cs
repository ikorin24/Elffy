#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Elffy.Imaging
{
    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct TgaHeader
    {
        [FieldOffset(0)]
        internal readonly byte IDLength;

        [FieldOffset(1)]
        internal readonly ByteBool HasColorMap;

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

    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct ByteBool : IEquatable<ByteBool>
    {
        [FieldOffset(0)]
        private readonly byte Value;

        public override bool Equals(object? obj) => obj is ByteBool b && Equals(b);

        public bool Equals(ByteBool other) => Value == other.Value;

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(ByteBool left, ByteBool right) => left.Equals(right);

        public static bool operator !=(ByteBool left, ByteBool right) => !(left == right);

        public override string ToString() => ((bool)this).ToString();

        public static implicit operator bool(ByteBool byteBool) => byteBool.Value != 0;
    }
}
