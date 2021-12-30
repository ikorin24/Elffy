#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct ContainerHeader
    {
        [FieldOffset(0)]
        private fixed byte _magic[8];
        [FieldOffset(0)]
        public ulong MagicWord;

        [FieldOffset(8)]
        public uint FormatVersion;
        [FieldOffset(12)]
        public ContainerType Type;
        [FieldOffset(20)]
        public ContainerCompressionType CompressionType;
        [FieldOffset(24)]
        public ulong ContentSize;
        [FieldOffset(32)]
        public ulong ContentDecompressedSize;

        public bool HasValidMagicWord()
        {
            return MagicWord == ValidMagicWord;
        }

        private static ReadOnlySpan<byte> _validMagic => new byte[8]
        {
            0x89,   // non-ascii charactor
            (byte)'E',
            (byte)'D',
            (byte)'C',
            0x0D,   // CR
            0x0A,   // LF
            0x1A,   // Ctrl-Z
            0x0A,   // LF
        };

        public static ulong ValidMagicWord => Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(_validMagic));
    }
}
