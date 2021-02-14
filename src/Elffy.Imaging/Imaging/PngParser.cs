#nullable enable

#if !NETCOREAPP3_1
#define CAN_SKIP_LOCALS_INIT
#endif

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
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
        public static Image Parse(Stream stream)
        {
            CheckSignature(stream);

            var buf = new BufferSpanReader(stackalloc byte[8]);
            using var data = new UnmanagedList<byte>();
            UnsafeEx.SkipInitIfPossible(out Header header);
            var palette = UnsafeRawArray<PngColor>.Empty;
            try {
                bool hasHeader = false;
                while(true) {
                    buf.Position = 0;
                    stream.SafeRead(buf.Span);
                    var chunkSize = buf.Next(4).Int32BigEndian();
                    var chunkType = buf.Next(4);

                    if(hasHeader == false) {
                        if(chunkType.SequenceEqual(ChunkTypeIHDR)) {
                            hasHeader = true;
                            ParseIHDR(stream, chunkSize, out header);
                            ValidateHeader(header);
                        }
                        else {
                            ThrowHelper.ThrowFormatException("No IHDR chunk");
                        }
                    }
                    else {
                        if(chunkType.SequenceEqual(ChunkTypeIDAT)) {
                            ParseIDAT(stream, chunkSize, data);
                        }
                        else if(chunkType.SequenceEqual(ChunkTypeIEND)) {
                            if(chunkSize != 0) {
                                ThrowHelper.ThrowFormatException("IEND chunk size must be 0");
                            }
                            break;
                        }
                        else if(chunkType.SequenceEqual(ChunkTypePLTE)) {
                            palette = ParsePLTE(stream, chunkSize);
                        }
                        else {
                            Debug.WriteLine(System.Text.Encoding.ASCII.GetString(chunkType) + " (skip)");
                            stream.SafeSkip(chunkSize);     // Skip not supported chunk
                        }
                    }
                    stream.SafeSkip(4);         // I don't validate CRC
                }

                Debug.Assert(hasHeader);
                return Decompress(data, header, palette.AsSpan());
            }
            finally {
                palette.Dispose();
            }
        }

#if CAN_SKIP_LOCALS_INIT
        [SkipLocalsInit]
#endif
        private static void CheckSignature(Stream stream)
        {
            Span<byte> pngSignature = stackalloc byte[8];
            stream.SafeRead(pngSignature);
            if(pngSignature.SequenceEqual(PngSignature) == false) {
                ThrowHelper.ThrowFormatException("PNG signature mismatch");
            }
        }

#if CAN_SKIP_LOCALS_INIT
        [SkipLocalsInit]
#endif
        private static void ParseIHDR(Stream stream, int chunkSize, out Header header)
        {
            Debug.WriteLine("IHDR");
            const int ChunkSize = 13;
            if(chunkSize != ChunkSize) {
                ThrowHelper.ThrowFormatException("Invalid IHDR chunk size");
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
                ThrowHelper.ThrowFormatException("Invalid header");
            }
            if(header.FilterType != FilterType.Default) {
                ThrowHelper.ThrowFormatException("Invalid header");
            }
            if(header.InterlaceType != InterlaceType.None && header.InterlaceType != InterlaceType.Adam7) {
                ThrowHelper.ThrowFormatException("Invalid header");
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
                ThrowHelper.ThrowFormatException("Invalid header");
            }
        }

        private static Image Decompress(UnmanagedList<byte> compressed, in Header header, ReadOnlySpan<PngColor> palette)
        {
            // | 1 byte  | 1 byte  | 0 or 4 bytes | N bytes ... |
            // |   CMF   |  CINFO  |    DICTID    |   data  ... |

            var cm = compressed[0] & 0x0f;
            if(cm != 8) {
                ThrowHelper.ThrowFormatException("Invalid zlib compression mode");
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
            return BuildPixels(buf.AsSpan(0, size), header, palette);
        }

        private static Image BuildPixels(ReadOnlySpan<byte> data, in Header header, ReadOnlySpan<PngColor> palette)
        {
            var image = new Image(header.Width, header.Height);
            try {
                switch(header.ColorType) {
                    case ColorType.Gray:
                        throw new NotImplementedException();
                    case ColorType.TrueColor:
                        BuildTrueColor(data, header, image.GetPixels());
                        break;
                    case ColorType.IndexColor: {
                        BuildIndexColor(data, header, palette, image.GetPixels());
                        break;
                    }
                    case ColorType.GrayAlpha:
                        throw new NotImplementedException();
                    case ColorType.TrueColorAlpha:
                        throw new NotImplementedException();
                    default:
                        ThrowHelper.ThrowFormatException();
                        break;
                }
                return image;
            }
            catch {
                image.Dispose();
                throw;
            }

            static void BuildIndexColor(ReadOnlySpan<byte> data, in Header header, ReadOnlySpan<PngColor> palette, Span<ColorByte> pixels)
            {
                var bits = header.Bits;
                var rowSize = (int)MathF.Ceiling((float)header.Width / (8 / bits)) + 1;
                for(int y = 0; y < header.Height; y++) {
                    // Index color mode can not use row-line filter.
                    //var type = (RowLineFilterType)data[y * rowSize];

                    var rowSrc = data.Slice(y * rowSize + 1, rowSize - 1);
                    var rowDst = pixels.Slice(y * header.Width, header.Width);

                    for(int x = 0; x < header.Width; x++) {
                        var index = BitArrayOperation.GetBitsValue(rowSrc, x, bits);
                        var c = palette[index];
                        ref var p = ref pixels[y * header.Width + x];
                        p.R = c.R;
                        p.G = c.G;
                        p.B = c.B;
                        p.A = 0xff;
                    }
                }
            }

            static void BuildTrueColor(ReadOnlySpan<byte> data, in Header header, Span<ColorByte> pixels)
            {
                // bits depth is 8 or 16
                if(header.Bits == 8) {
                    var rowSize = header.Width * 3 + 1;
                    for(int y = 0; y < header.Height; y++) {
                        var type = (RowLineFilterType)data[y * rowSize];
                        var rowSrc = data.Slice(y * rowSize + 1, rowSize - 1);
                        var rowDst = pixels.Slice(y * header.Width, header.Width);
                        switch(type) {
                            case RowLineFilterType.Sub: {
                                ColorByte prev = default;
                                for(int x = 0; x < header.Width; x++) {
                                    var x3 = x * 3;
                                    ref var p = ref pixels[y * header.Width + x];
                                    p.R = (byte)(rowSrc[x3] + prev.R);
                                    p.G = (byte)(rowSrc[x3 + 1] + prev.G);
                                    p.B = (byte)(rowSrc[x3 + 2] + prev.B);
                                    p.A = 0xff;
                                    prev = p;
                                }
                                break;
                            }
                            case RowLineFilterType.Up: {
                                if(y == 0) {
                                    for(int x = 0; x < header.Width; x++) {
                                        var x3 = x * 3;
                                        ref var p = ref pixels[y * header.Width + x];
                                        p.R = rowSrc[x3];
                                        p.G = rowSrc[x3 + 1];
                                        p.B = rowSrc[x3 + 2];
                                        p.A = 0xff;
                                    }
                                }
                                else {
                                    var prevRow = pixels.Slice((y - 1) * header.Width, header.Width);
                                    for(int x = 0; x < header.Width; x++) {
                                        var x3 = x * 3;
                                        ref var p = ref pixels[y * header.Width + x];
                                        ref var prev = ref prevRow.At(x);
                                        p.R = (byte)(rowSrc[x3] + prev.R);
                                        p.G = (byte)(rowSrc[x3 + 1] + prev.G);
                                        p.B = (byte)(rowSrc[x3 + 2] + prev.B);
                                        p.A = 0xff;
                                    }
                                }
                                break;
                            }
                            case RowLineFilterType.Average: {
                                if(y == 0) {
                                    ColorByte left = default;
                                    for(int x = 0; x < header.Width; x++) {
                                        var x3 = x * 3;
                                        ref var p = ref pixels[y * header.Width + x];
                                        p.R = (byte)(rowSrc[x3] + left.R);
                                        p.G = (byte)(rowSrc[x3 + 1] + left.G);
                                        p.B = (byte)(rowSrc[x3 + 2] + left.B);
                                        p.A = 0xff;
                                        left = p;
                                    }
                                }
                                else {
                                    ColorByte left = default;
                                    var upRow = pixels.Slice((y - 1) * header.Width, header.Width);
                                    for(int x = 0; x < header.Width; x++) {
                                        var x3 = x * 3;
                                        ref var p = ref pixels[y * header.Width + x];
                                        ref var up = ref upRow.At(x);
                                        p.R = (byte)(rowSrc[x3] + (up.R + left.R) / 2);
                                        p.G = (byte)(rowSrc[x3 + 1] + (up.G + left.G) / 2);
                                        p.B = (byte)(rowSrc[x3 + 2] + (up.B + left.B) / 2);
                                        p.A = 0xff;
                                        left = p;
                                    }
                                }
                                break;
                            }
                            case RowLineFilterType.Paeth: {
                                if(y == 0) {
                                    ColorByte left = default;
                                    for(int x = 0; x < header.Width; x++) {
                                        var x3 = x * 3;
                                        ref var p = ref pixels[y * header.Width + x];
                                        p.R = (byte)(rowSrc[x3] + left.R);
                                        p.G = (byte)(rowSrc[x3 + 1] + left.G);
                                        p.B = (byte)(rowSrc[x3 + 2] + left.B);
                                        p.A = 0xff;
                                        left = p;
                                    }
                                    break;
                                }
                                else {
                                    ColorByte left = default;
                                    ColorByte leftUp = default;
                                    var upRow = pixels.Slice((y - 1) * header.Width, header.Width);
                                    for(int x = 0; x < header.Width; x++) {
                                        var x3 = x * 3;
                                        ref var p = ref pixels[y * header.Width + x];
                                        ref var up = ref upRow.At(x);
                                        p.R = (byte)(rowSrc[x3] + Paeth(left.R, up.R, leftUp.R));
                                        p.G = (byte)(rowSrc[x3 + 1] + Paeth(left.G, up.G, leftUp.G));
                                        p.B = (byte)(rowSrc[x3 + 2] + Paeth(left.B, up.B, leftUp.B));
                                        p.A = 0xff;
                                        left = p;
                                        leftUp = up;
                                    }
                                }
                                break;
                            }
                            case RowLineFilterType.None: {
                                for(int x = 0; x < header.Width; x++) {
                                    var x3 = x * 3;
                                    ref var p = ref pixels[y * header.Width + x];
                                    p.R = rowSrc[x3];
                                    p.G = rowSrc[x3 + 1];
                                    p.B = rowSrc[x3 + 2];
                                    p.A = 0xff;
                                }
                                break;
                            }
                            default:
                                ThrowHelper.ThrowFormatException();
                                break;
                        }
                    }
                }
                else {
                    Debug.Assert(header.Bits == 16);
                    // TODO:
                    throw new NotImplementedException("16 bits depth color (48 bits RGB) is not implemented.");
                }

                static byte Paeth(byte left, byte up, byte leftUp)
                {
                    var p = left + up - leftUp;
                    int a = Math.Abs(p - left);
                    var b = Math.Abs(p - up);
                    var c = Math.Abs(p - leftUp);
                    return (a <= b && a <= c) ? left :
                           (b <= c) ? up : leftUp;
                }
            }
        }

        private static void ParseIDAT(Stream stream, int chunkSize, UnmanagedList<byte> data)
        {
            Debug.WriteLine("IDAT");
            var span = data.Extend(chunkSize, false);
            stream.SafeRead(span);
        }

        private static UnsafeRawArray<PngColor> ParsePLTE(Stream stream, int chunkSize)
        {
            Debug.WriteLine("PLTE");
            Debug.Assert(sizeof(PngColor) == 3);
            var colorCount = Math.DivRem(chunkSize, 3, out var mod3);
            if(mod3 != 0) {
                ThrowHelper.ThrowFormatException("Invalid PLTE chunk size.");
            }
            var palette = new UnsafeRawArray<PngColor>(colorCount);
            try {
                stream.SafeRead(palette.AsBytes());
                return palette;
            }
            catch {
                palette.Dispose();
                throw;
            }
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

        private enum RowLineFilterType : byte
        {
            None = 0,
            Sub = 1,
            Up = 2,
            Average = 3,
            Paeth = 4,
        }

        [StructLayout(LayoutKind.Explicit, Size = 3)]
        [DebuggerDisplay("(R={R}, G={G}, B={B})")]
        private readonly struct PngColor
        {
            [FieldOffset(0)]
            public readonly byte R;
            [FieldOffset(1)]
            public readonly byte G;
            [FieldOffset(2)]
            public readonly byte B;
        }
    }
}
