#nullable enable
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Elffy.Core
{
    internal static class ResourceInitializer
    {
        private const string FormatVersion = "1.0";
        private const string MagicWord = "ELFFY_RESOURCE";
        private static readonly Encoding _utf8 = Encoding.UTF8;

        public static Dictionary<string, ResourceObject> CreateDictionary(string filePath)
        {
            using(var fs = AlloclessFileStream.OpenRead(filePath))
            using(var pooledArray = new PooledArray<byte>(2048)) {
                var reader = new Reader(fs, pooledArray.InnerArray, _utf8);

                if(reader.ReadString(3) != FormatVersion) {
                    throw new FormatException();
                }
                if(reader.ReadString(MagicWord.Length) != MagicWord) {
                    throw new FormatException();
                }
                var fileCount = reader.ReadInt32();
                reader.Skip(sizeof(long));  // hash sum

                var dic = new Dictionary<string, ResourceObject>(fileCount);
                while(!reader.IsEndOfStream) {
                    //var res = new ResourceObject();
                    var name = reader.ReadString(reader.ReadInt32());
                    reader.Skip(sizeof(long));  // time stamp
                    var len = reader.ReadInt64();
                    var pos = reader.StreamPosition;
                    var res = new ResourceObject(len, pos);
                    dic.Add(name, res);
                    reader.Skip(res.Length);    // data
                }
                return dic;
            }
        }

        private readonly ref struct Reader
        {
            private readonly Stream _stream;
            private readonly byte[] _buf;
            private readonly Encoding _encoding;

            public bool IsEndOfStream => _stream.Position == _stream.Length;
            public long StreamPosition => _stream.Position;

            public Reader(Stream stream, byte[] buf, Encoding encoding)
            {
                _stream = stream;
                _buf = buf;
                _encoding = encoding;
            }

            public string ReadString(int byteLen)
            {
                if(_stream.Read(_buf, 0, byteLen) != byteLen) {
                    ThrowEOS();
                }
                return _encoding.GetString(_buf, 0, byteLen);
            }

            public void Skip(long byteLen)
            {
                _stream.Position += byteLen;
            }

            public int ReadInt32()
            {
                if(_stream.Read(_buf, 0, sizeof(int)) != sizeof(int)) {
                    ThrowEOS();
                }
                return BinaryPrimitives.ReadInt32LittleEndian(_buf);
            }

            public long ReadInt64()
            {
                if(_stream.Read(_buf, 0, sizeof(long)) != sizeof(long)) {
                    ThrowEOS();
                }
                return BinaryPrimitives.ReadInt64LittleEndian(_buf);
            }

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
