﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Elffy.Exceptions;
using Elffy.Serialization;
using Elffy.Shape;

namespace Elffy
{
    public static class Resources
    {
        #region private member
        internal const string RESOURCE_FILE_NAME = "Resources.dat";
        private const string FORMAT_VERSION = "1.0";
        /// <summary>正常解凍を確認するためのマジックワード</summary>
        private const string MAGIC_WORD = "ELFFY_RESOURCE";
        /// <summary>ハッシュのバイト長</summary>
        private const int HASH_LEN = 32;
        /// <summary>ファイルサイズのバイト長</summary>
        private const int FILE_SIZE_LEN = 8;
        /// <summary>AESの鍵生成時のsalt</summary>
        private static readonly Encoding _encoding = Encoding.UTF8;
        private static Dictionary<string, ResourceObject> _resources;
        private static byte[] _bufEntity;
        private static byte[] _buf => _bufEntity ?? (_bufEntity = new byte[1024 * 1024]);
        #endregion

        internal static bool IsInitialized { get; private set; }

        #region Initialize
        internal static void Initialize()
        {
            try {
                CreateDictionary();
            }
            catch(Exception ex) {
                _resources?.Clear();
                _resources = null;
                throw new FormatException("Failed in creating resource dic.",ex);
            }
            IsInitialized = true;
        }
        #endregion

        public static ICollection<string> GetResourceNames()
        {
            CheckInitialized();
            return _resources.Keys;
        }
            
        #region LoadStream
        public static ResourceStream LoadStream(string name)
        {
            CheckInitialized();
            if(name == null) { throw new ArgumentNullException(); }
            if(!_resources.TryGetValue(name, out var resource)) {
                throw new ResourceNotFoundException(name);
            }
            return new ResourceStream(resource.Position, resource.Length);
        }
        #endregion

        public static Model3D LoadModel(string name)
        {
            CheckInitialized();
            var stream = LoadStream(name);
            var ext = Path.GetExtension(name).ToLower();
            switch(ext) {
                case ".fbx":
                    return ModelLoader.Load(stream, ModelType.Fbx);
                default:
                    throw new NotSupportedException($"Extension '{ext}' is not supported.");
            }
        }

        internal static bool HasResource(string name)
        {
            CheckInitialized();
            return _resources.ContainsKey(name);
        }

        #region CreateDictionary
        private static void CreateDictionary()
        {
            if(!File.Exists(RESOURCE_FILE_NAME)) {
                _resources = new Dictionary<string, ResourceObject>(0);
                return;
            }
            int BytesToIntLittleEndian(byte[] x) => Enumerable.Range(0, 4).Select(i => x[i] << (i * 8)).Sum();
            const byte END_MARK = 0x3A;
            using(var fs = File.OpenRead(RESOURCE_FILE_NAME)) {
                // フォーマットバージョンの確認
                if(ReadString(fs, 3) != FORMAT_VERSION) { throw new FormatException(); }
                // マジックワードの確認
                if(ReadString(fs, MAGIC_WORD.Length) != MAGIC_WORD) { throw new FormatException(); }
                fs.Read(_buf, 0, 4);
                var fileCount = BytesToIntLittleEndian(_buf);       // ファイル数取得
                _resources = new Dictionary<string, ResourceObject>(fileCount);
                while(true) {
                    if(fs.Position == fs.Length) { break; }     // ファイル末尾で終了
                    var resource = new ResourceObject();
                    resource.Name = ReadString(fs, END_MARK);                                       // ファイル名取得
                    if(fs.Read(_buf, 0, HASH_LEN) != HASH_LEN) { throw new FormatException(); }     // ハッシュ値を読み飛ばす(使わない)
                    //resource.Length = ReadLong(fs, END_MARK);                                       // ファイル長取得
                    resource.Length = (fs.Read(_buf, 0, FILE_SIZE_LEN) == FILE_SIZE_LEN) ? BytesToLongLittleEndian(_buf) : throw new FormatException(); // ファイル長取得
                    resource.Position = fs.Position;
                    fs.Position += resource.Length;             // データ部を読み飛ばす
                    _resources.Add(resource.Name, resource);
                }
            }
        }
        #endregion

        #region ReadString
        private static string ReadString(Stream stream, int byteCount)
        {
            stream.Read(_buf, 0, byteCount);
            return _encoding.GetString(_buf, 0, byteCount);
        }

        private static string ReadString(Stream stream, byte endMark)
        {
            var bufPos = 0;
            while(true) {
                var tmp = stream.ReadByte();
                if(tmp == -1) { throw new FormatException(); }     // ファイル末尾ならフォーマットエラー
                var b = (byte)tmp;
                if(b == endMark) { break; }        // 区切り文字なら終了
                _buf[bufPos++] = b;
            }
            return _encoding.GetString(_buf, 0, bufPos);
        }
        #endregion

        private static long BytesToLongLittleEndian(byte[] x) => Enumerable.Range(0, sizeof(long)).Select(i => ((long)x[i]) << (i * 8)).Sum();
        private static int ReadInt(Stream stream, byte endMark) => int.Parse(ReadString(stream, endMark));
        private static long ReadLong(Stream stream, byte endMark) => long.Parse(ReadString(stream, endMark));
        private static void CheckInitialized()
        {
            if(!IsInitialized) { throw new InvalidOperationException("Resources not Initialized"); }
        }

        #region class ResourceObject
        private class ResourceObject
        {
            public string Name { get; set; }
            public long Length { get; set; }
            public long Position { get; set; }
            public override string ToString() => $"'{Name}', Length:{Length}, Position:{Position}";
        }
        #endregion
    }

    #region class ResourceStream
    public sealed class ResourceStream : Stream, IDisposable
    {
        private bool _disposed = false;
        private FileStream _innerStream;
        private readonly long _head;
        private readonly long _length;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position
        {
            get => _innerStream.Position - _head;
            set
            {
                if(value >= _length || value <= 0) { throw new ArgumentOutOfRangeException(); }
                _innerStream.Position =  _head + value;
            }
        }
        
        internal ResourceStream(long head, long length)
        {
            _head = head;
            _length = length;
            _innerStream = new FileStream(Resources.RESOURCE_FILE_NAME, FileMode.Open) { Position = _head };
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if(_disposed) { throw new ObjectDisposedException(nameof(ResourceStream)); }
            var current = _innerStream.Position;
            var available = (int)(_head + _length - current);
            return _innerStream.Read(buffer, offset, Math.Min(count, available));
        }

        protected override void Dispose(bool disposing)
        {
            if(_disposed) { throw new ObjectDisposedException(nameof(ResourceStream)); }
            if(disposing) {
                // マネージリソース解放
                _innerStream.Dispose();
                _innerStream = null;
            }
            // アンマネージドリソースがある場合ここに解放処理を書く
            _disposed = true;
            base.Dispose(disposing);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
    #endregion

#if false
    // 暗号化されたリソース用に作ったが、ひとまず暗号化はやめたが消すのはもったいないのでこのクラスは使わない
    public static class ___Resources
    {
    #region private member
        internal const string RESOURCE_FILE_NAME = "Resources.dat";
        private const string FORMAT_VERSION = "1.0";
        /// <summary>暗号化された状態でのマジックワードのバイト長</summary>
        private const int ENCRYPTED_MAGIC_WORD_LEN = 16;
        /// <summary>正常解凍を確認するためのマジックワード</summary>
        private const string MAGIC_WORD = "ELFFY_RESOURCE";
        /// <summary>AESのキーのバイト数</summary>
        private const int AES_KEY_SIZE = 16;
        /// <summary>初期化ベクトルのバイト数</summary>
        private const int IV_SIZE = 16;
        /// <summary>ハッシュのバイト長</summary>
        private const int HASH_LEN = 32;
        /// <summary>AESの鍵生成時のsalt</summary>
        private static readonly byte[] _salt = new byte[16]
        {
            0x6A, 0x10, 0xAF, 0xAC,
            0x45, 0xA6, 0xA7, 0xCF,
            0x69, 0x41, 0xE5, 0x0B,
            0xF6, 0x95, 0xBD, 0x99,
        };
        private static readonly Encoding _encoding = Encoding.UTF8;
        private static string _password;
        private static byte[] _aesKey;
        private static byte[] _iv;
        private static Dictionary<string, ResourceObject> _resources;
        private static byte[] _bufEntity;
        private static byte[] _buf => _bufEntity ?? (_bufEntity = new byte[1024 * 1024]);
    #endregion

        internal static bool IsInitialized { get; private set; }

        public static byte[] Load(string name)
        {
            CheckInitialized();
            if(name == null) { throw new ArgumentNullException(nameof(name)); }
            if(!_resources.TryGetValue(name, out var resource)) {
                throw new ResourceNotFoundException(name);
            }
            try {
                using(var fs = File.OpenRead(RESOURCE_FILE_NAME))
                using(var aes = new AesManaged() { BlockSize = 128, KeySize = 128, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7, Key = _aesKey, IV = _iv })
                using(var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using(var ms = new MemoryStream(resource.Length > int.MaxValue ? int.MaxValue : (int)resource.Length)) {
                    using(var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write)) {
                        fs.Position = resource.Position;
                        var allLen = resource.Length;
                        while(true) {
                            if(allLen <= 0) { break; }
                            var readRequestLen = (int)(_buf.Length < allLen ? _buf.Length : allLen);
                            var readlen = fs.Read(_buf, 0, readRequestLen);
                            if(readlen != readRequestLen) { throw new ResourceLoadFailedException(name, null); }
                            allLen -= readlen;
                            cs.Write(_buf, 0, readlen);
                        }
                    }
                    return ms.ToArray();
                }
            }
            catch(Exception ex) {
                throw new ResourceLoadFailedException(name, ex);
            }
        }

    #region Initialize
        internal static void Initialize(string password)
        {
            if(string.IsNullOrEmpty(password)) { throw new ArgumentException($"{nameof(password)} is null or empty."); }
            GenerateAesKey(password, out _aesKey, out _iv);
            _password = password;
            try {
                CreateDictionary();
            }
            catch(Exception ex) {
                _resources?.Clear();
                _resources = null;
                throw ex;
            }
            IsInitialized = true;
        }

        internal static void Initialize()
        {
            _resources = new Dictionary<string, ResourceObject>(0);
            IsInitialized = true;
        }
    #endregion

        private static void CheckInitialized()
        {
            if(!IsInitialized) { throw new InvalidOperationException($"Resource is not initialized. Call {nameof(Initialize)} method before."); }
        }

    #region GenerateAesKey
        /// <summary>パスワードからAESの鍵を生成します</summary>
        /// <param name="password">パスワード</param>
        /// <param name="aesKey">生成されたAESの鍵</param>
        /// <param name="iv">生成されたAESの初期化ベクトル</param>
        private static void GenerateAesKey(string password, out byte[] aesKey, out byte[] iv)
        {
            // パスワードにsaltを付与してaesの鍵を生成します。ついでに初期化ベクトルも一緒に生成します。
            using(var deriveBytes = new Rfc2898DeriveBytes(password, _salt)) {
                aesKey = deriveBytes.GetBytes(AES_KEY_SIZE);
                iv = deriveBytes.GetBytes(IV_SIZE);
            }
        }
    #endregion

    #region CreateDictionary
        private static void CreateDictionary()
        {
            if(!File.Exists(RESOURCE_FILE_NAME)) {
                _resources = new Dictionary<string, ResourceObject>(0);
                return;
            }
            using(var fs = File.OpenRead(RESOURCE_FILE_NAME)) {
                // フォーマットバージョンの確認
                if(ReadFromStream(fs, 3) != FORMAT_VERSION) { throw new FormatException(); }
                
                using(var aes = new AesManaged() { BlockSize = 128, KeySize = 128, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7, Key = _aesKey, IV = _iv })
                using(var decryptor = aes.CreateDecryptor(aes.Key, aes.IV)) {
                    // マジックワードの確認
                    if(fs.Read(_buf, 0, ENCRYPTED_MAGIC_WORD_LEN) != ENCRYPTED_MAGIC_WORD_LEN) { throw new FormatException(); }
                    using(var ms = new MemoryStream()) {
                        using(var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write)) {
                            cs.Write(_buf, 0, ENCRYPTED_MAGIC_WORD_LEN);
                        }
                        try {
                            var magicWord = _encoding.GetString(ms.ToArray());
                            if(magicWord != MAGIC_WORD) { throw new FormatException(); }
                        }
                        catch(Exception) { throw new FormatException(); }
                    }

                    _resources = new Dictionary<string, ResourceObject>();
                    while(true) {
                        if(fs.Position == fs.Length) { break; }     // ファイル末尾で終了
                        var resource = new ResourceObject();
                        // ファイル名取得
                        int bufPos = 0;
                        while(true) {
                            var tmp = fs.ReadByte();
                            if(tmp == -1) { throw new FormatException(); }     // ファイル末尾ならフォーマットエラー
                            var b = (byte)tmp;
                            if(b == 0x3A) { break; }        // 区切り文字 0x3A == ':' までがファイル名
                            _buf[bufPos++] = b;
                        }
                        resource.Name = _encoding.GetString(_buf, 0, bufPos);

                        // ハッシュ値を読む(使わないので読み飛ばす)
                        if(fs.Read(_buf, 0, HASH_LEN) != HASH_LEN) { throw new FormatException(); }

                        // 読み取るバイト長を取得
                        bufPos = 0;
                        while(true) {
                            var tmp = fs.ReadByte();
                            if(tmp == -1) { throw new FormatException(); }      // ファイル末尾ならフォーマットエラー
                            var b = (byte)tmp;
                            if(b == 0x3A) { break; }        // 区切り文字 0x3A == ':' までがファイル名
                            _buf[bufPos++] = b;
                        }
                        if(!long.TryParse(_encoding.GetString(_buf, 0, bufPos), out long filelen)) { throw new FormatException(); }
                        resource.Length = filelen;
                        // ファイル開始位置を取得
                        resource.Position = fs.Position;
                        // データ部を読み飛ばす
                        fs.Position += filelen;
                        _resources.Add(resource.Name, resource);
                    }
                }
            }
        }
    #endregion

    #region ReadFromString
        private static string ReadFromStream(Stream stream, int byteCount)
        {
            stream.Read(_buf, 0, byteCount);
            return _encoding.GetString(_buf, 0, byteCount);
        }
    #endregion

    #region class ResourceObject
        private class ResourceObject
        {
            public string Name { get; set; }
            public long Length { get; set; }
            public long Position { get; set; }
            public override string ToString() => $"'{Name}', Length:{Length}, Position:{Position}";
        }
    #endregion

        public class ResourceNotFoundException : Exception
        {
            public string Name { get; private set; }

            internal ResourceNotFoundException(string name) : base($"Resouce '{name}' is not found") => Name = name;
        }

        public class ResourceLoadFailedException : Exception
        {
            public string Name { get; private set; }

            internal ResourceLoadFailedException(string name, Exception innerException) 
                : base($"Failed in loading resource '{name}'", innerException) => Name = name;
        }
    }
#endif
}