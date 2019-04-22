using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace ElffyResource
{
    public static class ResourceManager
    {
        #region private Member
        private const string FORMAT_VERSION = "1.0";
        /// <summary>一時ファイル名</summary>
        private const string TMP_FILE = "____tmp____";
        /// <summary>パスワードからAESのキーを生成する時のsaltのバイト数</summary>
        private const int SALT_SIZE = 16;
        /// <summary>AESのキーのバイト数</summary>
        private const int AES_KEY_SIZE = 16;
        /// <summary>初期化ベクトルのバイト数</summary>
        private const int IV_SIZE = 16;
        /// <summary>正常解凍を確認するためのマジックワード</summary>
        private const string MAGIC_WORD = "ELFFY_RESOURCE";
        /// <summary>暗号化された状態でのマジックワードのバイト長</summary>
        private const int ENCRYPTED_MAGIC_WORD_LEN = 32;

        private const int BUF_LEN = 1024 * 1024;
        /// <summary>AESの鍵生成時のsalt</summary>
        private static readonly byte[] _salt = new byte[16]
        {
            0x6A, 0x10, 0xAF, 0xAC,
            0x45, 0xA6, 0xA7, 0xCF,
            0x69, 0x41, 0xE5, 0x0B,
            0xF6, 0x95, 0xBD, 0x99,
        };
        /// <summary>文字のエンコード</summary>
        private static readonly Encoding _encoding = Encoding.UTF8;
        /// <summary>ファイル読み込み用のバッファ</summary>
        private static byte[] _buf;
        #endregion

        #region Build
        /// <summary>リソースのビルドを行います。</summary>
        /// <param name="directory">リソースディレクトリのパス</param>
        /// <param name="outputPath">出力ファイル名</param>
        /// <param name="password">暗号化に用いるパスワード</param>
        public static void Build(string directory, string outputPath, string password)
        {
            if(directory == null) { throw new ArgumentNullException(nameof(directory)); }
            if(outputPath == null) { throw new ArgumentNullException(nameof(outputPath)); }
            if(string.IsNullOrEmpty(password)) { throw new ArgumentException(nameof(password)); }
            if(!Directory.Exists(directory)) { throw new DirectoryNotFoundException($"directoryName : {directory}"); }
            try {
                _buf = new byte[BUF_LEN];
                GenerateAesKey(password, out var aesKey, out var iv);

                if(File.Exists(outputPath)) {
                    File.Delete(outputPath);
                }

                using(var fs = File.OpenWrite(outputPath))
                using(var aes = new AesManaged() { BlockSize = 128, KeySize = 128, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7, Key = aesKey, IV = iv })
                using(var encryptor = aes.CreateEncryptor(aes.Key, aes.IV)) {
                    WriteToStream(fs, FORMAT_VERSION);                          // フォーマットバージョンを出力へ書きこむ
                    using(var ms = new MemoryStream()) {
                        using(var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using(var dfs = new DeflateStream(cs, CompressionMode.Compress)) {
                            WriteToStream(dfs, MAGIC_WORD);                     // マジックワードを圧縮・暗号化してメモリストリームへ書き込む
                        }
                        var bytes = ms.ToArray();
                        fs.Write(bytes, 0, bytes.Length);                       // 暗号化されたマジックワードを出力へ書き込む
                    }
                    WriteDirectory(new DirectoryInfo(directory), "", fs, encryptor);
                }
                if(File.Exists(TMP_FILE)) {
                    File.Delete(TMP_FILE);
                }
            }
            finally {
                _buf = null;
            }
        }
        #endregion

        public static bool Decompress(string inputPath, string outputDirectory, string password)
        {
            if(outputDirectory == null) { throw new ArgumentNullException(nameof(outputDirectory)); }
            if(inputPath == null) { throw new ArgumentNullException(nameof(inputPath)); }
            if(string.IsNullOrEmpty(password)) { throw new ArgumentException(nameof(password)); }
            if(!File.Exists(inputPath)) { throw new FileNotFoundException($"file : {inputPath}"); }
            try {
                _buf = new byte[BUF_LEN];
                GenerateAesKey(password, out var aesKey, out var iv);
                using(var fs = File.OpenRead(inputPath))
                using(var aes = new AesManaged() { BlockSize = 128, KeySize = 128, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7, Key = aesKey, IV = iv })
                using(var decryptor = aes.CreateDecryptor(aes.Key, aes.IV)) {
                    // フォーマットバージョンの確認
                    var formatVersion = ReadFromStream(fs, 3);
                    if(formatVersion != FORMAT_VERSION) { return false; }

                    // マジックワードの確認
                    byte[] encryptedMagicWord = new byte[ENCRYPTED_MAGIC_WORD_LEN];
                    if(fs.Read(_buf, 0, ENCRYPTED_MAGIC_WORD_LEN) != ENCRYPTED_MAGIC_WORD_LEN) { return false; }
                    Array.Copy(_buf, 0, encryptedMagicWord, 0, ENCRYPTED_MAGIC_WORD_LEN);
                    using(var ms = new MemoryStream()) {
                        using(var dfs = new DeflateStream(ms, CompressionMode.Decompress))
                        using(var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write)) {
                            cs.Write(encryptedMagicWord, 0, encryptedMagicWord.Length);
                            cs.FlushFinalBlock();
                            var magicWord = _encoding.GetString(ms.ToArray());
                            //var magicWord = ReadFromStream(ms, (int)ms.Length);
                        }
                    }

                    // ディレクトリへの展開
                    ReadDirectory(fs, decryptor);
                }
            }
            finally {
                _buf = null;
            }
            return true;
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

        #region WriteDirectory
        /// <summary>ディレクトリを指定して、その内容を出力します</summary>
        /// <param name="dir">ディレクトリ情報</param>
        /// <param name="dirName">ディレクトリ名(ファイル名の前につける相対パス)</param>
        /// <param name="stream">出力するStream</param>
        /// <param name="encryptor">暗号化用のオブジェクト</param>
        private static void WriteDirectory(DirectoryInfo dir, string dirName, Stream stream, ICryptoTransform encryptor)
        {
            const long LARGE_FILE_SIZE = 50 * 1024 * 1024;

            // 巨大ファイルの暗号化時の節メモリのため、一度暗号化したバイト列を一時ファイルに随時書き出し、
            // 暗号化完了後に一時ファイルの内容を出力ファイルに書き出す。(ただし低速)
            // 小さいファイルの場合はMemoryStreamを使用する。(高速)
            foreach(var file in dir.GetFiles()) {
                WriteToStream(stream, $"'{dirName}{file.Name}'");       // ファイル名を出力
                if(file.Length > LARGE_FILE_SIZE) {
                    long fileLen = 0;
                    using(var tmpFs = File.OpenWrite(TMP_FILE))
                    using(var cs = new CryptoStream(tmpFs, encryptor, CryptoStreamMode.Write))
                    using(var dfs = new DeflateStream(cs, CompressionMode.Compress))
                    using(var fileReader = file.OpenRead()) {
                        while(true) {
                            var readlen = fileReader.Read(_buf, 0, _buf.Length);
                            if(readlen == 0) { break; }
                            dfs.Write(_buf, 0, readlen);
                        }
                        cs.FlushFinalBlock();       // CryptoStream.Dispose()時に呼ばれる処理だがファイル長取得の為に先に行っておく(ここで行うとDispose()時にはもう呼ばれないので問題ない)
                        fileLen = tmpFs.Length;
                    }
                    WriteToStream(stream, $"{fileLen.ToString()}:");    // ファイル長を書き込み

                    // 一時ファイルの内容を出力ストリームに書き込む(一時ファイルは使いまわしているので、その内容全てがこのファイルの暗号化バイト列ではないことに注意)
                    using(var tmpFs = File.OpenRead(TMP_FILE)) {
                        int totalLen = 0;
                        while(true) {
                            var readlen = tmpFs.Read(_buf, 0, _buf.Length);
                            totalLen += readlen;
                            if(totalLen >= fileLen) {
                                stream.Write(_buf, 0, (int)(fileLen - totalLen + readlen));
                                break;
                            }
                            stream.Write(_buf, 0, readlen);
                        }
                    }
                }
                else {
                    using(var ms = new MemoryStream())
                    using(var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using(var dfs = new DeflateStream(cs, CompressionMode.Compress))
                    using(var fileReader = file.OpenRead()) {
                        while(true) {
                            var readlen = fileReader.Read(_buf, 0, _buf.Length);
                            if(readlen == 0) { break; }
                            dfs.Write(_buf, 0, readlen);
                        }
                        cs.FlushFinalBlock();       // CryptoStream.Dispose()時に呼ばれる処理だがMemoryStreamの内容を確定させるために先に呼ぶ
                        var data = ms.ToArray();
                        WriteToStream(stream, $"{data.Length.ToString()}:");    // ファイル長を書き込み
                        stream.Write(data, 0, data.Length);                     // 暗号化されたバイト列を出力ストリームへ書き込む
                    }
                }
            }
            foreach(var subDir in dir.GetDirectories()) {
                WriteDirectory(subDir, $"{dirName}{subDir.Name}/", stream, encryptor);
            }
        }
        #endregion

        private static void ReadDirectory(Stream stream, ICryptoTransform decryptor)
        {
            
        }

        #region WriteToStream
        private static void WriteToStream(Stream stream, string str)
        {
            var bytes = _encoding.GetBytes(str);
            stream.Write(bytes, 0, bytes.Length);
        }
        #endregion

        #region ReadFromString
        private static string ReadFromStream(Stream stream, int byteCount)
        {
            stream.Read(_buf, 0, byteCount);
            return _encoding.GetString(_buf, 0, byteCount);
        }
        #endregion
    }
}
