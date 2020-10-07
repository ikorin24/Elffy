#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace ElffyResourceCompiler
{
    internal readonly ref struct LightBinaryWriter
    {
        private static readonly Encoding _utf8 = Encoding.UTF8;

        private readonly Stream _stream;

        public long Position { get => _stream.Position; set => _stream.Position = value; }

        public long Length => _stream.Length;

        public Stream InnerStream => _stream;

        public LightBinaryWriter(Stream stream)
        {
            _stream = stream;
        }

        public void WriteAsUTF8(string value)
        {
            // [NOTE]
            // Too long string causes stack over flow.

            var buf = ArrayPool<byte>.Shared.Rent(_utf8.GetMaxByteCount(value.Length));
            try {
                var len = _utf8.GetBytes(value, buf);
                _stream.Write(buf, 0, len);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }

        public void WriteLittleEndian(int value)
        {
            const int size = 4;
            var buf = ArrayPool<byte>.Shared.Rent(size);
            try {
                var span = buf.AsSpan(0, size);
                BinaryPrimitives.WriteInt32LittleEndian(span, value);
                _stream.Write(span);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }

        public void WriteLittleEndian(long value)
        {
            const int size = 8;
            var buf = ArrayPool<byte>.Shared.Rent(size);
            try {
                var span = buf.AsSpan(0, size);
                BinaryPrimitives.WriteInt64LittleEndian(span, value);
                _stream.Write(span);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }

        public void Write(byte[] value)
        {
            _stream.Write(value, 0, value.Length);
        }

        public void Write(byte[] value, int offset, int count)
        {
            _stream.Write(value, offset, count);
        }
    }
}
