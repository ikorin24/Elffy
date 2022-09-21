#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;
using System.Buffers;

namespace Elffy;

internal static class LocalResourceInitializer
{
    private static ReadOnlySpan<byte> MagicWord => "ELFFYRES"u8;
    private static ReadOnlySpan<byte> FormatVersion => "1000"u8;

    [SkipLocalsInit]
    public static Dictionary<string, ResourceObject> CreateDictionary(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        Span<byte> header = stackalloc byte[16];
        stream.ReadExactly(header);

        var magicWord = header[0..8];
        var formatVersion = header[8..12];

        if(magicWord.SequenceEqual(MagicWord) == false) {
            throw new FormatException("Invalid resource file fomrat");
        }
        if(formatVersion.SequenceEqual(FormatVersion) == false) {
            throw new NotSupportedException("Invalid format version");
        }
        uint fileCount = BinaryPrimitives.ReadUInt32LittleEndian(header[12..16]);
        var dic = new Dictionary<string, ResourceObject>((int)Math.Min(fileCount, int.MaxValue));
        for(uint i = 0; i < fileCount; i++) {
            var nameByteLen = ReadUInt32(stream);
            var name = ReadUtf8String(stream, nameByteLen);
            var size = ReadUInt64(stream);
            var offset = ReadUInt64(stream);

            if(size > long.MaxValue) { throw new NotSupportedException(); }
            if(offset > long.MaxValue) { throw new NotSupportedException(); }
            dic.Add(name, new()
            {
                Length = (long)size,
                Position = (long)offset,
            });
        }
        return dic;
    }

    [SkipLocalsInit]
    private static uint ReadUInt32(Stream stream)
    {
        Span<byte> buf = stackalloc byte[sizeof(uint)];
        stream.ReadExactly(buf);
        return BinaryPrimitives.ReadUInt32LittleEndian(buf);
    }

    [SkipLocalsInit]
    private static ulong ReadUInt64(Stream stream)
    {
        Span<byte> buf = stackalloc byte[sizeof(ulong)];
        stream.ReadExactly(buf);
        return BinaryPrimitives.ReadUInt64LittleEndian(buf);
    }

    [SkipLocalsInit]
    private static string ReadUtf8String(Stream stream, uint byteLen)
    {
        const int Threshold = 256;

        if(byteLen > int.MaxValue) { throw new NotSupportedException(); }
        if(byteLen <= Threshold) {
            Span<byte> buf = stackalloc byte[Threshold];
            var data = buf[..(int)byteLen];
            stream.ReadExactly(data);
            return Encoding.UTF8.GetString(data);
        }
        else {
            var buf = ArrayPool<byte>.Shared.Rent((int)byteLen);
            try {
                var data = buf[..(int)byteLen];
                stream.ReadExactly(data);
                return Encoding.UTF8.GetString(data);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }
    }
}

internal record struct ResourceObject
{
    public required long Length { get; init; }
    public required long Position { get; init; }

    public ResourceObject(long length, long position)
    {
        Length = length;
        Position = position;
    }

    public override string ToString() => $"Length:{Length}, Position:{Position}";
}
