#nullable enable
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace ElffyResourceCompiler
{
    internal readonly ref struct LightBinaryReader
    {
        private static readonly Encoding _utf8 = Encoding.UTF8;

        private readonly Stream _stream;

        public long Position { get => _stream.Position; set => _stream.Position = value; }

        public long Length => _stream.Length;

        public Stream InnerStream => _stream;

        public LightBinaryReader(Stream stream)
        {
            _stream = stream;
        }

        public string ReadUTF8(int byteLength)
        {
            var buf = ArrayPool<byte>.Shared.Rent(byteLength);
            try {
                var readlen = _stream.Read(buf, 0, byteLength);
                if(readlen != byteLength) {
                    throw new EndOfStreamException();
                }
                return _utf8.GetString(buf, 0, readlen);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }

        public string ReadTerminatedString(byte termination = 0x00)
        {
            var buf = ArrayPool<byte>.Shared.Rent(4 * 1024);
            try {
                int bufPos = 0;
                while(true) {
                    var tmp = _stream.ReadByte();
                    if(tmp == -1) {

                    }
                    var b = (byte)tmp;
                    if(b == termination) { break; }
                    buf[bufPos++] = b;
                }
                return _utf8.GetString(buf, 0, bufPos);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }

        public int ReadInt32()
        {
            const int size = 4;
            Span<byte> buf = stackalloc byte[size];
            var readlen = _stream.Read(buf);
            if(readlen != size) {
                throw new EndOfStreamException();
            }
            return BinaryPrimitives.ReadInt32LittleEndian(buf);
        }

        public long ReadInt64()
        {
            const int size = 8;
            Span<byte> buf = stackalloc byte[size];
            var readlen = _stream.Read(buf);
            if(readlen != size) {
                throw new EndOfStreamException();
            }
            return BinaryPrimitives.ReadInt64LittleEndian(buf);
        }

        public void Read(Span<byte> buffer)
        {
            var len = _stream.Read(buffer);
            if(len != buffer.Length) {
                throw new EndOfStreamException();
            }
        }

        public void CopyBytesTo(Stream dest, long length)
        {
            var allLen = length;
            var buf = ArrayPool<byte>.Shared.Rent(1024 * 1024);
            try {
                while(true) {
                    if(allLen <= 0) { break; }
                    var readRequestLen = (int)Math.Min(buf.Length, allLen);
                    var readlen = _stream.Read(buf, 0, readRequestLen);
                    if(readlen != readRequestLen) {
                        throw new EndOfStreamException();
                    }
                    allLen -= readlen;
                    dest.Write(buf, 0, readlen);
                }
            }
            finally {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }
    }
}
