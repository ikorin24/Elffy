#nullable enable

#if !NETCOREAPP3_1
#define CAN_SKIP_LOCALS_INIT
#endif

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using UnmanageUtility;
using System.Diagnostics;
using System.IO.Compression;
using Elffy.Imaging.Internal;

namespace Elffy.Imaging
{
    public static unsafe class PngParser
    {
        private static ReadOnlySpan<byte> ChunkTypeIHDR => new byte[] { (byte)'I', (byte)'H', (byte)'D', (byte)'R', };
        private static ReadOnlySpan<byte> ChunkTypeIDAT => new byte[] { (byte)'I', (byte)'D', (byte)'A', (byte)'T', };
        private static ReadOnlySpan<byte> ChunkTypePLTE => new byte[] { (byte)'P', (byte)'L', (byte)'T', (byte)'E', };
        private static ReadOnlySpan<byte> ChunkTypeIEND => new byte[] { (byte)'I', (byte)'E', (byte)'N', (byte)'D', };

        internal static ReadOnlySpan<byte> PngSignature => new byte[8]
        {
            0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a,
        };

#if CAN_SKIP_LOCALS_INIT
        [SkipLocalsInit]
#endif
        public static RawImage Parse(Stream stream)
        {
            CheckSignature(stream);

            var buf = new BufferSpanReader(stackalloc byte[8]);
            using var data = new UnmanagedList<byte>();
            var hasHeader = false;
#if CAN_SKIP_LOCALS_INIT
            Unsafe.SkipInit(out Header header);
#else
            Header header = default;
#endif
            while(true) {
                buf.Position = 0;
                stream.SafeRead(buf.Span);
                var chunkSize = buf.Next(4).Int32BigEndian();
                var chunkType = buf.Next(4);

                if(chunkType.SequenceEqual(ChunkTypeIHDR)) {
                    hasHeader = true;
                    ParseIHDR(stream, chunkSize, out header);
                }
                else if(chunkType.SequenceEqual(ChunkTypeIDAT)) {
                    ParseIDAT(stream, chunkSize, data);
                }
                else if(chunkType.SequenceEqual(ChunkTypeIEND)) {
                    if(chunkSize != 0) {
                        throw new Exception();
                    }
                    break;
                }
                else {
                    stream.SafeSkip(chunkSize);
                }
                stream.SafeSkip(4);         // I don't validate CRC
            }

            if(!hasHeader) { throw new Exception(); }
            ValidateHeader(header);
            BuildPixels(data, header);

            throw new NotImplementedException();
        }

#if CAN_SKIP_LOCALS_INIT
        [SkipLocalsInit]
#endif
        private static void CheckSignature(Stream stream)
        {
            Span<byte> pngSignature = stackalloc byte[8];
            stream.SafeRead(pngSignature);
            if(pngSignature.SequenceEqual(PngSignature) == false) {
                throw new InvalidDataException("data is not png format.");
            }
        }

#if CAN_SKIP_LOCALS_INIT
        [SkipLocalsInit]
#endif
        private static void ParseIHDR(Stream stream, int chunkSize, out Header header)
        {
            const int ChunkSize = 13;
            if(chunkSize != ChunkSize) {
                throw new Exception();
            }
            var buf = new BufferSpanReader(stackalloc byte[ChunkSize]);
            stream.SafeRead(buf.Span);

            header.Width = buf.Next(4).Int32BigEndian();                // 4 bytes
            header.Height = buf.Next(4).Int32BigEndian();               // 4 bytes
            header.Bits = buf.NextByte();                               // 1 bytes
            header.ColorType = (ColorType)buf.NextByte();               // 1 bytes
            header.CompressionType = (CompressionType)buf.NextByte();   // 1 bytes
            header.FilterType = (FilterType)buf.NextByte();             // 1 bytes
            header.InterlaceType = (InterlaceType)buf.NextByte();       // 1 bytes
            Debug.Assert(buf.Position == buf.Length);
        }

        private static void ValidateHeader(in Header header)
        {
            if(header.CompressionType != CompressionType.Default) {
                throw new Exception();
            }
            if(header.FilterType != FilterType.Default) {
                throw new Exception();
            }
            if(header.InterlaceType != InterlaceType.None && header.InterlaceType != InterlaceType.Adam7) {
                throw new Exception();
            }
            var isValid = header.ColorType switch
            {
                ColorType.Gray => header.Bits switch { 1 or 2 or 4 or 8 or 16 => true, _ => false, },
                ColorType.TrueColor => header.Bits switch { 8 or 16 => true, _ => false, },
                ColorType.IndexColor => header.Bits switch { 1 or 2 or 4 or 8 => true, _ => false, },
                ColorType.GrayAlpha => header.Bits switch { 8 or 16 => true, _ => false, },
                ColorType.TrueColorAlpha => header.Bits switch { 8 or 16 => true, _ => false, },
                _ => false,
            };
            if(!isValid) {
                throw new Exception();
            }
        }

        private static void BuildPixels(UnmanagedList<byte> compressed, in Header header)
        {
            // | 1 byte  | 1 byte  | 0 or 4 bytes | N bytes ... |
            // |   CMF   |  CINFO  |    DICTID    |   data  ... |

            var cm = compressed[0] & 0x0f;
            if(cm != 8) {
                throw new Exception();
            }
            //var cinfo = (compressed[0] & 0xf0) >> 4;  // I don't care about 'cinfo'
            var fdict = (compressed[1] & 0b_00100000) == 0b_00100000;

            void* ptr;
            int len;
            if(fdict) {
                // I don't care about 'DICTID'
                ptr = ((byte*)compressed.Ptr) + 5;
                len = compressed.Count - 5;
            }
            else {
                ptr = ((byte*)compressed.Ptr) + 2;
                len = compressed.Count - 2;
            }

            using var ptrStream = PointerStream.Create(ptr, len);
            using var deflateStream = new DeflateStream(ptrStream, CompressionMode.Decompress);
            using var buf = new UnmanagedList<byte>(capacity: len);
            var end = false;
            var size = 0;
            while(!end) {
                var span = buf.Extend(2048, false);
                var readlen = deflateStream.Read(span);
                size += readlen;
                end = readlen == 0;
            }
            var decompressed = buf.AsSpan(0, size);
            return;
        }

        private static void ParseIDAT(Stream stream, int chunkSize, UnmanagedList<byte> data)
        {
            var span = data.Extend(chunkSize, false);
            stream.SafeRead(span);
        }

        public static RawImage Parse(ReadOnlySpan<byte> data)
        {
            if(data.Slice(0, PngSignature.Length).SequenceEqual(PngSignature) == false) {
                throw new InvalidDataException("data is not png format.");
            }
            return ParseCore(data);
        }

        internal static RawImage ParseCore(ReadOnlySpan<byte> data)
        {


            throw new NotImplementedException("Png parser is not implemented");
        }


        private struct Header
        {
            public int Width;
            public int Height;
            public byte Bits;
            public ColorType ColorType;
            public CompressionType CompressionType;
            public FilterType FilterType;
            public InterlaceType InterlaceType;
        }

        private enum ColorType : byte
        {
            Gray = 0,
            TrueColor = 2,
            IndexColor = 3,
            GrayAlpha = 4,
            TrueColorAlpha = 6,
        }

        private enum CompressionType : byte
        {
            Default = 0,
        }

        private enum FilterType : byte
        {
            Default = 0,
        }

        private enum InterlaceType : byte
        {
            None = 0,
            Adam7 = 1,
        }
    }
}
