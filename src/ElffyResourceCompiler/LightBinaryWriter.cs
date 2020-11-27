#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
            var buf = ArrayPool<byte>.Shared.Rent(_utf8.GetMaxByteCount(value.Length));
            try {
                var len = _utf8.GetBytes(value, 0, value.Length, buf, 0);
                _stream.Write(buf, 0, len);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }

        public void WriteAsUTF8WithLength(string value)
        {
            var buf = ArrayPool<byte>.Shared.Rent(_utf8.GetMaxByteCount(value.Length));
            try {
                var len = _utf8.GetBytes(value, 0, value.Length, buf, 0);
                WriteLittleEndian(len);
                _stream.Write(buf, 0, len);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }

        public void WriteLittleEndian(int value)
        {
            const int size = 4;
            Span<byte> b = stackalloc byte[size];
            BinaryPrimitives.WriteInt32LittleEndian(b, value);
            _stream.Write(b);
        }

        public void WriteLittleEndian(long value)
        {
            const int size = 8;
            Span<byte> b = stackalloc byte[size];
            BinaryPrimitives.WriteInt64LittleEndian(b, value);
            _stream.Write(b);
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

#if NETSTANDARD2_0
    internal static class StreamExtenson
    {
        public static void Write(this Stream source, ReadOnlySpan<byte> buffer)
        {
            byte[]? array = null;
            try {
                array = ArrayPool<byte>.Shared.Rent(buffer.Length);
                buffer.CopyTo(array.AsSpan());
                source.Write(array, 0, buffer.Length);
            }
            finally {
                if(array is not null) {
                    ArrayPool<byte>.Shared.Return(array);
                }
            }
        }

        public static int Read(this Stream source, Span<byte> buffer)
        {
            byte[]? array = null;
            try {
                array = ArrayPool<byte>.Shared.Rent(buffer.Length);
                var readlen = source.Read(array, 0, buffer.Length);
                array.AsSpan(0, buffer.Length).CopyTo(buffer);
                return readlen;
            }
            finally {
                if(array is not null) {
                    ArrayPool<byte>.Shared.Return(array);
                }
            }
        }
    }
#endif
}
