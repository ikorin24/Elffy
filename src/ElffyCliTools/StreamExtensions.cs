#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Buffers.Binary;
using System.Text;
using System.Buffers;

namespace ElffyCliTools;

internal static class StreamExtensions
{
    [SkipLocalsInit]
    public static void WriteUtf8WithLength(this Stream stream, ReadOnlySpan<char> str)
    {
        var utf8 = Encoding.UTF8;

        var len = utf8.GetByteCount(str);
        if(len <= 128) {
            Span<byte> buf = stackalloc byte[128];
            var l = utf8.GetBytes(str, buf);
            stream.WriteUInt32LE((uint)l);
            stream.Write(buf[..l]);
        }
        else {
            var buf = ArrayPool<byte>.Shared.Rent(len);
            try {
                var l = utf8.GetBytes(str, buf);
                stream.WriteUInt32LE((uint)l);
                stream.Write(buf[..l]);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }
    }

    [SkipLocalsInit]
    public static void WriteUInt32LE(this Stream stream, uint value)
    {
        Span<byte> buf = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(buf, value);
        stream.Write(buf);
    }

    [SkipLocalsInit]
    public static void WriteUInt64LE(this Stream stream, ulong value)
    {
        Span<byte> buf = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(buf, value);
        stream.Write(buf);
    }
}
