#nullable enable
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using StringLiteral;

namespace Elffy.Core
{
    internal static partial class ResourceInitializer
    {
        [Utf8("1.0")]
        private static partial ReadOnlySpan<byte> FormatVersion();

        [Utf8("ELFFY_RESOURCE")]
        private static partial ReadOnlySpan<byte> MagicWord();

        public static Dictionary<string, ResourceObject> CreateDictionary(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var pooledArray = new PooledArray<byte>(2048);

            var reader = new Reader(stream, pooledArray.InnerArray);
            if(!reader.ReadAndEqual(FormatVersion())) {
                throw new FormatException();
            }
            if(!reader.ReadAndEqual(MagicWord())) {
                throw new FormatException();
            }
            var fileCount = reader.ReadInt32();
            reader.Skip(sizeof(long));      // hash sum

            var utf8 = Encoding.UTF8;
            var dic = new Dictionary<string, ResourceObject>(fileCount);
            while(!reader.IsEndOfStream) {
                var name = reader.ReadString(reader.ReadInt32(), utf8);
                reader.Skip(sizeof(long));  // time stamp
                var len = reader.ReadInt64();
                var pos = reader.StreamPosition;
                var res = new ResourceObject(len, pos);
                dic.Add(name, res);
                reader.Skip(res.Length);    // data
            }
            return dic;
        }

        private readonly ref struct Reader
        {
            // [NOTE]
            // Use FileStream.Read(Span<byte>) for performance.
            // The method does not allocate inner buffer.

            private readonly FileStream _stream;
            private readonly byte[] _buf;

            public bool IsEndOfStream => _stream.Position == _stream.Length;
            public long StreamPosition => _stream.Position;

            public Reader(FileStream stream, byte[] buf)
            {
                _stream = stream;
                _buf = buf;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<byte> Read(int byteLen)
            {
                var span = _buf.AsSpan(0, byteLen);
                if(_stream.Read(span) != byteLen) {
                    ThrowEOS();
                }
                return span;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ReadAndEqual(ReadOnlySpan<byte> data)
            {
                return Read(data.Length).SequenceEqual(data);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public string ReadString(int byteLen ,Encoding encoding)
            {
                return encoding.GetString(Read(byteLen));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Skip(long byteLen)
            {
                _stream.Position += byteLen;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int ReadInt32()
            {
                return BinaryPrimitives.ReadInt32LittleEndian(Read(sizeof(int)));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public long ReadInt64()
            {
                return BinaryPrimitives.ReadInt64LittleEndian(Read(sizeof(long)));
            }

            [DoesNotReturn]
            private static void ThrowEOS() => throw new EndOfStreamException();
        }
    }

    internal struct ResourceObject
    {
        public long Length { get; }
        public long Position { get; }

        public ResourceObject(long length, long position)
        {
            Length = length;
            Position = position;
        }

        public override string ToString() => $"Length:{Length}, Position:{Position}";
    }
}
