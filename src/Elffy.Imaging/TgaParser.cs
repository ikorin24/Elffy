#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Imaging
{
    public static class TgaParser
    {
        public static unsafe Bitmap Parse(Stream stream)
        {
            if(stream is null) { throw new ArgumentNullException(nameof(stream)); }

            using var reader = new ZBinaryReader(stream);
            ParseHeader(reader, out var header);
            var bitmap = new Bitmap(header.Width, header.Height, PixelFormat.Format32bppArgb);
            using var pixels = bitmap.GetPixels(ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try {
                ParseID(reader, header);
                ParseColorMap(reader, header);
                ParseData(reader, header, pixels.AsSpan());
                ParseFooter(reader, header);
            }
            catch(Exception ex) {
#if DEBUG
                // Fill all pixels white for debug.
                Debug.Fail(
@$"[ERROR] Failed loading tga image pixels !!!
    {ex.GetType().Name}
Ignore to use white-filled bitmap instead.");

                pixels.AsSpan().Fill(byte.MaxValue);
#else
                pixels.Dispose();
                bitmap.Dispose();
                throw ex;
#endif
            }
            return bitmap;
        }

        private static unsafe void ParseHeader(in ZBinaryReader reader, out TgaHeader header)
        {
            fixed(void* p = &header) {
                var buf = new Span<byte>(p, sizeof(TgaHeader));
                reader.ReadBytes(buf);
            }
            if(header.BitsPerPixel != 24 && header.BitsPerPixel != 32) {
                throw new InvalidDataException($"Invalid bits per pixel. : bpp = {header.BitsPerPixel}");
            }
        }

        private static void ParseID(in ZBinaryReader reader, in TgaHeader header)
        {
            // Skip this chunk. Read none.
            reader.Skip(header.IDLength);
        }

        private static void ParseColorMap(in ZBinaryReader reader, in TgaHeader header)
        {
            if(header.HasColorMap) { throw new NotImplementedException("Parsing color map is not implemented."); }
        }

        private static void ParseData(in ZBinaryReader reader, in TgaHeader header, in Span<byte> pixels)
        {
            switch(header.Format) {
                case TgaDataFormat.NoImage:
                    return;
                case TgaDataFormat.IndexedColor:
                    throw new NotImplementedException();
                case TgaDataFormat.FullColor:
                    ParseFullColor(reader, header, pixels);
                    return;
                case TgaDataFormat.Gray:
                    throw new NotImplementedException();
                case TgaDataFormat.IndexedColorRLE:
                    throw new NotImplementedException();
                case TgaDataFormat.FullColorRLE:
                    throw new NotImplementedException();
                case TgaDataFormat.GrayRLE:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }

        private static void ParseFooter(in ZBinaryReader reader, in TgaHeader header)
        {
            // nop
        }

        private static void ParseFullColor(in ZBinaryReader reader, in TgaHeader header, in Span<byte> pixels)
        {
            var pixels32 = MemoryMarshal.Cast<byte, uint>(pixels);
            if(header.BitsPerPixel == 32) {
                if(header.IsTopToBottom) {
                    if(header.IsLeftToRight) {
                        for(int y = 0; y < header.Height; y++) {
                            for(int x = 0; x < header.Width; x++) {
                                pixels32[y * header.Width + x] = reader.ReadUInt32();               // 4 bytes  (B, G, R, A)
                            }
                        }
                    }
                    else {
                        for(int y = 0; y < header.Height; y++) {
                            for(int x = header.Width - 1; x >= 0; x--) {    // reverse X
                                pixels32[y * header.Width + x] = reader.ReadUInt32();               // 4 bytes  (B, G, R, A)
                            }
                        }

                    }
                }
                else {
                    if(header.IsLeftToRight) {
                        for(int y = header.Height - 1; y >= 0; y--) {       // reverse Y
                            for(int x = 0; x < header.Width; x++) {
                                pixels32[y * header.Width + x] = reader.ReadUInt32();               // 4 bytes  (B, G, R, A)
                            }
                        }
                    }
                    else {
                        for(int y = header.Height - 1; y >= 0; y--) {       // reverse Y
                            for(int x = header.Width - 1; x >= 0; x--) {    // reverse X
                                pixels32[y * header.Width + x] = reader.ReadUInt32();               // 4 bytes  (B, G, R, A)
                            }
                        }

                    }
                }
            }
            else {   // 24 bits
                Debug.Assert(header.BitsPerPixel == 24);
                Span<byte> bgra = stackalloc byte[4];
                bgra[3] = byte.MaxValue;
                var bgr = bgra.Slice(0, 3);

                if(header.IsTopToBottom) {
                    if(header.IsLeftToRight) {
                        for(int y = 0; y < header.Height; y++) {
                            for(int x = 0; x < header.Width; x++) {
                                reader.ReadBytes(bgr);
                                pixels32[y * header.Width + x] = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(bgra));
                            }
                        }
                    }
                    else {
                        for(int y = 0; y < header.Height; y++) {
                            for(int x = header.Width - 1; x >= 0; x--) {    // reverse X
                                reader.ReadBytes(bgr);
                                pixels32[y * header.Width + x] = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(bgra));
                            }
                        }

                    }
                }
                else {
                    if(header.IsLeftToRight) {
                        for(int y = header.Height - 1; y >= 0; y--) {       // reverse Y
                            for(int x = 0; x < header.Width; x++) {
                                reader.ReadBytes(bgr);
                                pixels32[y * header.Width + x] = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(bgra));
                            }
                        }
                    }
                    else {
                        for(int y = header.Height - 1; y >= 0; y--) {       // reverse Y
                            for(int x = header.Width - 1; x >= 0; x--) {    // reverse X
                                reader.ReadBytes(bgr);
                                pixels32[y * header.Width + x] = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(bgra));
                            }
                        }

                    }
                }
            }
        }
    }

    internal readonly struct ZBinaryReader : IDisposable, IEquatable<ZBinaryReader>
    {
        private readonly byte[] _buf;
        private readonly Stream _stream;

        public readonly Stream InnerStream => _stream ?? throw new ObjectDisposedException(nameof(ZBinaryReader));

        public ZBinaryReader(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _buf = ArrayPool<byte>.Shared.Rent(32);
        }

        public readonly void Skip(int byteLength)
        {
            if(byteLength < 0) { throw new ArgumentOutOfRangeException(nameof(byteLength)); }
            if(byteLength == 0) { return; }

            var total = byteLength;
            while(true) {
                var readlen = Math.Min(_buf.Length, total);
                ReadToBuf(readlen);
                total -= readlen;
            }
        }

        public readonly byte ReadByte()
        {
            ReadToBuf(sizeof(byte));
            return _buf[0];
        }

        public readonly void ReadBytes(Span<byte> dest)
        {
            ReadToBuf(dest.Length);
            _buf.AsSpan(0, dest.Length).CopyTo(dest);
        }

        public readonly int ReadInt32()
        {
            ReadToBuf(sizeof(int));
            return Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(_buf.AsSpan()));
        }

        public readonly uint ReadUInt32() => (uint)ReadInt32();

        private readonly void ReadToBuf(int readByteLen)
        {
            // Must be readByteLen <= _buf.Length
            Debug.Assert(readByteLen <= _buf.Length);
            if(_stream.Read(_buf, 0, readByteLen) != readByteLen) { throw new EndOfStreamException(); }
        }

        public void Dispose()
        {
            // Not dispose inner stream
            Unsafe.AsRef(_stream) = null!;
            ArrayPool<byte>.Shared.Return(_buf);
            Unsafe.AsRef(_buf) = null!;
        }

        public override bool Equals(object? obj) => obj is ZBinaryReader reader && Equals(reader);

        public bool Equals(ZBinaryReader other)
        {
            return EqualityComparer<byte[]>.Default.Equals(_buf, other._buf) &&
                   EqualityComparer<Stream>.Default.Equals(_stream, other._stream);
        }

        public override int GetHashCode() => HashCode.Combine(_buf, _stream);
    }
}
