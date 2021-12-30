#nullable enable
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using System;

namespace Elffy
{
    internal unsafe static partial class DataContainerSerializer
    {
        private const uint CurrentFormatVersion = 1;

        public static ref readonly ContainerHeader ReadHeader(ReadOnlySpan<byte> binary)
        {
            CheckPlatformEndian();
            if(binary.Length < sizeof(ContainerHeader)) {
                throw new ArgumentException("Header data is too short.");
            }
            ref readonly var header = ref UnsafeEx.As<byte, ContainerHeader>(binary.GetReference());

            if(header.HasValidMagicWord() == false) {
                throw new ArgumentException("Invalid container header");
            }
            var contentSize = header.ContentSize;
            if(contentSize > int.MaxValue) {
                throw new NotSupportedException("Large size content is not supported.");
            }
            return ref header;
        }

        private static void CheckPlatformEndian()
        {
            if(BitConverter.IsLittleEndian == false) { throw new PlatformNotSupportedException("Big endian platform is not supported."); }
        }

        private static UniquePtr Decompress(in ContainerHeader header, UniquePtr content, ulong contentByteLength, out ulong decompressedByteLength)
        {
            try {
                if(header.CompressionType == ContainerCompressionType.None) {
                    decompressedByteLength = header.ContentDecompressedSize;
                    return content.Move();
                }
                else if(header.CompressionType == ContainerCompressionType.Deflate) {
                    decompressedByteLength = header.ContentDecompressedSize;
                    using var decompressed = UniquePtr.Malloc((nuint)decompressedByteLength);
                    throw new NotImplementedException();
                }
                else {
                    throw new NotSupportedException();
                }
            }
            finally {
                content.Dispose();
            }
        }
    }
}
