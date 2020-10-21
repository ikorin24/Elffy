#nullable enable
using Elffy.AssemblyServices;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Elffy
{
    public static class Resources
    {
        private const string FORMAT_VERSION = "1.0";
        private const string MAGIC_WORD = "ELFFY_RESOURCE";
        private static readonly Encoding _utf8 = Encoding.UTF8;
        private static Dictionary<string, ResourceObject>? _resources;
        private static bool _isInitialized;
        private static ResourceLoader? _loader;
        private static string? _resourceFilePath;

        public static ResourceLoader Loader
        {
            get
            {
                CheckInitialized();
                Debug.Assert(_loader is null == false);
                return _loader!;
            }
        }

        public static void Initialize(string resourceFilePath)
        {
            if(resourceFilePath is null) {
                throw new ArgumentNullException(nameof(resourceFilePath));
            }
            if(!File.Exists(resourceFilePath)) {
                throw new FileNotFoundException("Resource file not found", resourceFilePath);
            }
            if(_isInitialized) {
                throw new InvalidOperationException("Already initialized");
            }


            _resourceFilePath = resourceFilePath;
            try {
                CreateDictionary(resourceFilePath);
                _loader = new ResourceLoader();
            }
            catch(Exception ex) {
                _resources?.Clear();
                _resources = null;
                throw new FormatException("Failed in creating resource dic.", ex);
            }
            _isInitialized = true;
        }

        /// <summary>リソースを読み込むストリームを取得します</summary>
        /// <param name="name">リソース名</param>
        /// <returns>リソースのストリーム</returns>
        public static ResourceStream GetStream(string name)
        {
            CheckInitialized();
            if(name is null) {
                ThrowNullArg();
                static void ThrowNullArg() => throw new ArgumentNullException(nameof(name));
            }

            if(_resources!.TryGetValue(name!, out var resource) == false) {
                throw new ResourceNotFoundException(name!);
            }
            Debug.Assert(_resourceFilePath is null == false);
            return new ResourceStream(_resourceFilePath, resource.Position, resource.Length);
        }

        //internal static bool HasResource(string name)
        //{
        //    // this method is only for debug

        //    CheckInitialized();
        //    return _resources!.ContainsKey($"{RESOURCE_ROOT}/{name}");
        //}

        internal static string[] GetResourceNames()
        {
            // this method is only for debug

            CheckInitialized();
            return _resources!.Keys.ToArray();
        }

        private static void CreateDictionary(string filePath)
        {
            if(!File.Exists(filePath)) {
                _resources = new Dictionary<string, ResourceObject>(0);
                return;
            }

            using(var fs = AlloclessFileStream.OpenRead(filePath))
            using(var pooledArray = new PooledArray<byte>(2048)) {
                var buf = pooledArray.InnerArray;
                if(ReadString(fs, 3, buf) != FORMAT_VERSION) {
                    throw new FormatException();
                }
                if(ReadString(fs, MAGIC_WORD.Length, buf) != MAGIC_WORD) {
                    throw new FormatException();
                }
                fs.Read(buf, 0, 4);
                var fileCount = BinaryPrimitives.ReadInt32LittleEndian(buf);
                fs.Position += sizeof(long);    // hashSum
                _resources = new Dictionary<string, ResourceObject>(fileCount);
                while(true) {
                    if(fs.Position == fs.Length) { break; }
                    var resource = new ResourceObject();
                    resource.Name = ReadStringWithLength(fs, buf);
                    fs.Position += sizeof(long);    // time stamp
                    resource.Length = (fs.Read(buf, 0, sizeof(long)) == sizeof(long)) ? BinaryPrimitives.ReadInt64LittleEndian(buf)
                                                                                      : throw new FormatException();
                    resource.Position = fs.Position;
                    fs.Position += resource.Length;         // data
                    _resources.Add(resource.Name, resource);
                }
            }
        }

        private static string ReadString(Stream stream, int byteCount, byte[] buf)
        {
            stream.Read(buf, 0, byteCount);
            return _utf8.GetString(buf, 0, byteCount);
        }

        private static string ReadStringWithLength(Stream stream, byte[] buf)
        {
            if(stream.Read(buf, 0, sizeof(int)) != sizeof(int)) {
                throw new EndOfStreamException();
            }
            var len = BinaryPrimitives.ReadInt32LittleEndian(buf);
            if(stream.Read(buf, 0, len) != len) {
                throw new EndOfStreamException();
            }
            return _utf8.GetString(buf, 0, len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckInitialized()
        {
            if(!_isInitialized) {
                ThrowNotInitialized();
                static void ThrowNotInitialized() => throw new InvalidOperationException("Resources not Initialized");
            }
        }

        private class ResourceObject
        {
            public string Name { get; set; } = string.Empty;
            public long Length { get; set; }
            public long Position { get; set; }
            public override string ToString() => $"'{Name}', Length:{Length}, Position:{Position}";
        }
    }
}
