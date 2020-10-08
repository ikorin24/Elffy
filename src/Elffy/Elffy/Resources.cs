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

namespace Elffy
{
    public static class Resources
    {
        internal const string RESOURCE_FILE_NAME = "Resources.dat";
        private const string FORMAT_VERSION = "1.0";
        /// <summary>正常解凍を確認するためのマジックワード</summary>
        private const string MAGIC_WORD = "ELFFY_RESOURCE";
        /// <summary>ハッシュのバイト長</summary>
        private const int HASH_LEN = 32;
        /// <summary>ファイルサイズのバイト長</summary>
        private const int FILE_SIZE_LEN = 8;
        private static readonly Encoding _encoding = Encoding.UTF8;
        private static Dictionary<string, ResourceObject>? _resources;

        internal static readonly string ResourceFilePath = Path.Combine(AssemblyState.EntryAssemblyDirectory, RESOURCE_FILE_NAME);

        internal static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            if(IsInitialized) { return; }
            try {
                CreateDictionary();
            }
            catch(Exception ex) {
                _resources?.Clear();
                _resources = null;
                throw new FormatException("Failed in creating resource dic.", ex);
            }
            IsInitialized = true;
        }

        public static ReadOnlySpan<char> GetDirectoryName(string name)
        {
            if(name is null) {
                ThrowNullArg();
                static void ThrowNullArg() => throw new ArgumentNullException(nameof(name));
            }
            for(int i = name!.Length - 1; i >= 0; i--) {
                if(name[i] == '/') {
                    return name.AsSpan(0, i);
                }
            }
            return ReadOnlySpan<char>.Empty;
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

            // TODO: ReadOnlySpan<char> をキーにした専用 dictionary の実装
            //var fullname = $"{RESOURCE_ROOT}/{name}";

            if(_resources!.TryGetValue(name!, out var resource) == false) {
                throw new ResourceNotFoundException(name!);
            }
            return new ResourceStream(resource.Position, resource.Length);
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

        private static void CreateDictionary()
        {
            if(!File.Exists(ResourceFilePath)) {
                _resources = new Dictionary<string, ResourceObject>(0);
                return;
            }

            using(var fs = AlloclessFileStream.OpenRead(ResourceFilePath))
            using(var pooledArray = new PooledArray<byte>(2048)) {
                var buf = pooledArray.InnerArray;
                // フォーマットバージョンの確認
                if(ReadString(fs, 3, buf) != FORMAT_VERSION) { throw new FormatException(); }
                // マジックワードの確認
                if(ReadString(fs, MAGIC_WORD.Length, buf) != MAGIC_WORD) { throw new FormatException(); }
                fs.Read(buf, 0, 4);
                var fileCount = BinaryPrimitives.ReadInt32LittleEndian(buf);
                fs.Position += HASH_LEN;    // hashSum
                _resources = new Dictionary<string, ResourceObject>(fileCount);
                while(true) {
                    if(fs.Position == fs.Length) { break; }     // ファイル末尾で終了
                    var resource = new ResourceObject();
                    resource.Name = ReadStringWithLength(fs, buf);
                    if(fs.Read(buf, 0, HASH_LEN) != HASH_LEN) { throw new FormatException(); }     // ハッシュ値を読み飛ばす(使わない)
                    resource.Length = (fs.Read(buf, 0, FILE_SIZE_LEN) == FILE_SIZE_LEN) ? BinaryPrimitives.ReadInt64LittleEndian(buf) : throw new FormatException(); // ファイル長取得
                    resource.Position = fs.Position;
                    fs.Position += resource.Length;             // データ部を読み飛ばす
                    _resources.Add(resource.Name, resource);
                }
            }
        }

        private static string ReadString(Stream stream, int byteCount, byte[] buf)
        {
            stream.Read(buf, 0, byteCount);
            return _encoding.GetString(buf, 0, byteCount);
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
            return _encoding.GetString(buf, 0, len);
        }

        private static string ReadString(Stream stream, byte endMark, byte[] buf)
        {
            var bufPos = 0;
            while(true) {
                var tmp = stream.ReadByte();
                if(tmp == -1) { throw new FormatException(); }     // ファイル末尾ならフォーマットエラー
                var b = (byte)tmp;
                if(b == endMark) { break; }        // 区切り文字なら終了
                buf[bufPos++] = b;
            }
            return _encoding.GetString(buf, 0, bufPos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckInitialized()
        {
            if(!IsInitialized) {
                ThrowNotInitialized();
                static void ThrowNotInitialized() => throw new InvalidOperationException("Resources not Initialized");
            }
        }

        // ReadOnlySpan<char> をキーにした専用 dictionary を作った時に
        // 構造体にして ref でやり取りすべき
        private class ResourceObject
        {
            public string Name { get; set; } = string.Empty;
            public long Length { get; set; }
            public long Position { get; set; }
            public override string ToString() => $"'{Name}', Length:{Length}, Position:{Position}";
        }
    }
}
