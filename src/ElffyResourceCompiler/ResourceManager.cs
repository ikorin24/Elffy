using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace ElffyResourceCompiler
{
    public static class ResourceManager
    {
        #region private Member
        private const string FORMAT_VERSION = "1.0";
        /// <summary>一時ファイル名</summary>
        private const string TMP_FILE = "____tmp____";
        /// <summary>一時ディレクトリ名</summary>
        private const string TMP_DIR = "____tmp_dir____";
        /// <summary>AESのキーのバイト数</summary>
        private const int AES_KEY_SIZE = 16;
        /// <summary>初期化ベクトルのバイト数</summary>
        private const int IV_SIZE = 16;
        /// <summary>正常解凍を確認するためのマジックワード</summary>
        private const string MAGIC_WORD = "ELFFY_RESOURCE";
        /// <summary>暗号化された状態でのマジックワードのバイト長</summary>
        private const int ENCRYPTED_MAGIC_WORD_LEN = 16;
        /// <summary>大きなファイルの閾値(Byte)</summary>
        private const long LARGE_FILE_SIZE = 50 * 1024 * 1024;
        /// <summary>バッファ長</summary>
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
        /// <summary>ファイルのハッシュ値計算用</summary>
        private static readonly HashAlgorithm _hashFunc = new SHA256CryptoServiceProvider();
        /// <summary>ハッシュのバイト長</summary>
        private const int HASH_LEN = 32;
        #endregion

        #region Compile
        /// <summary>リソースのビルドを行います。</summary>
        /// <param name="targetDir">リソースディレクトリのパス</param>
        /// <param name="outputPath">出力ファイル名</param>
        /// <param name="password">暗号化に用いるパスワード</param>
        public static void Compile(string targetDir, string outputPath, string password)
        {
            if(targetDir == null) { throw new ArgumentNullException(nameof(targetDir)); }
            if(outputPath == null) { throw new ArgumentNullException(nameof(outputPath)); }
            if(string.IsNullOrEmpty(password)) { throw new ArgumentException(nameof(password)); }
            try {
                _buf = new byte[BUF_LEN];
                GenerateAesKey(password, out var aesKey, out var iv);
                if(File.Exists(outputPath)) { File.Delete(outputPath); }
                Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);
                using(var fs = File.OpenWrite(outputPath))
                using(var aes = new AesManaged() { BlockSize = 128, KeySize = 128, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7, Key = aesKey, IV = iv })
                using(var encryptor = aes.CreateEncryptor(aes.Key, aes.IV)) {
                    WriteToStream(fs, FORMAT_VERSION);                          // フォーマットバージョンを出力へ書きこむ
                    using(var ms = new MemoryStream()) {
                        using(var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)) {
                            WriteToStream(cs, MAGIC_WORD);                      // マジックワードを暗号化してメモリストリームへ書き込む
                        }
                        var bytes = ms.ToArray();
                        fs.Write(bytes, 0, bytes.Length);                       // 暗号化されたマジックワードを出力へ書き込む
                    }
                    var dir = new DirectoryInfo(targetDir);
                    if(!Directory.Exists(dir.FullName)) { return; }
                    WriteDirectory(dir, "", fs, encryptor);
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

        public static void DiffCompile(string directory, string outputPath, string password)
        {
            throw new NotImplementedException();

            if(directory == null) { throw new ArgumentNullException(nameof(directory)); }
            if(outputPath == null) { throw new ArgumentNullException(nameof(outputPath)); }
            if(string.IsNullOrEmpty(password)) { throw new ArgumentException(nameof(password)); }
            if(!Directory.Exists(directory)) { throw new DirectoryNotFoundException($"directoryName : {directory}"); }
            if(!File.Exists(outputPath)) {
                Compile(directory, outputPath, password);           // 出力ファイルが存在しないなら全コンパイル
                return;
            }
        }

        #region Decompile
        /// <summary>リソースを解凍します</summary>
        /// <param name="inputPath">解凍するリソースのパス</param>
        /// <param name="outputDirectory">出力ディレクトリ</param>
        /// <param name="password">復号パスワード</param>
        /// <returns></returns>
        public static bool Decompile(string inputPath, string outputDirectory, string password)
        {
            if(outputDirectory == null) { throw new ArgumentNullException(nameof(outputDirectory)); }
            if(inputPath == null) { throw new ArgumentNullException(nameof(inputPath)); }
            if(string.IsNullOrEmpty(password)) { throw new ArgumentException(nameof(password)); }
            if(!File.Exists(inputPath)) { throw new FileNotFoundException($"file : {inputPath}"); }
            try {
                _buf = new byte[BUF_LEN];
                if(Directory.Exists(outputDirectory)) {
                    if(Directory.Exists(TMP_DIR)) { Directory.Delete(TMP_DIR, true); }
                    Directory.Move(outputDirectory, TMP_DIR);       // 出力先ディレクトリが既に存在するなら一時退避
                }
                Directory.CreateDirectory(outputDirectory);
                GenerateAesKey(password, out var aesKey, out var iv);
                using(var fs = File.OpenRead(inputPath))
                using(var aes = new AesManaged() { BlockSize = 128, KeySize = 128, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7, Key = aesKey, IV = iv })
                using(var decryptor = aes.CreateDecryptor(aes.Key, aes.IV)) {
                    // フォーマットバージョンの確認
                    var formatVersion = ReadFromStream(fs, 3);
                    if(formatVersion != FORMAT_VERSION) { return false; }

                    // マジックワードの確認
                    if(fs.Read(_buf, 0, ENCRYPTED_MAGIC_WORD_LEN) != ENCRYPTED_MAGIC_WORD_LEN) { return false; }
                    using(var ms = new MemoryStream()) {
                        using(var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write)) {
                            cs.Write(_buf, 0, ENCRYPTED_MAGIC_WORD_LEN);
                        }
                        try {
                            var magicWord = _encoding.GetString(ms.ToArray());
                            if(magicWord != MAGIC_WORD) { return false; }
                        }
                        catch(Exception) { return false; }
                    }

                    // ディレクトリへの展開
                    ReadDirectory(fs, decryptor, new DirectoryInfo(outputDirectory));
                }
            }
            catch(Exception ex) {
                if(Directory.Exists(TMP_DIR)) {
                    Directory.Move(TMP_DIR, outputDirectory);       // 退避させた元のディレクトリを復元
                }
                throw ex;
            }
            finally {
                _buf = null;
            }
            if(Directory.Exists(TMP_DIR)) {
                Directory.Delete(TMP_DIR, true);          // 退避させた一時ディレクトリがあるなら消す
            }
            return true;
        }
        #endregion

        #region private Method
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
            // 巨大ファイルの暗号化時の節メモリのため、一度暗号化したバイト列を一時ファイルに随時書き出し、
            // 暗号化完了後に一時ファイルの内容を出力ファイルに書き出す。(ただし低速)
            // 小さいファイルの場合はMemoryStreamを使用する。(高速)
            foreach(var file in dir.GetFiles()) {
                WriteToStream(stream, $"{dirName}{file.Name}:");       // ファイル名を出力

                // ファイルハッシュの書き込み
                using(var fs = file.OpenRead()) {
                    var hash = _hashFunc.ComputeHash(fs);
                    stream.Write(hash, 0, hash.Length);
                }
                
                // データ部の書き込み
                if(file.Length > LARGE_FILE_SIZE) {
                    long fileLen = 0;
                    using(var tmpFs = File.OpenWrite(TMP_FILE)) {       // 既に同名のファイルが存在しているとき、書き込んだ部分だけがファイルに上書きされる
                        using(var cs = new CryptoStream(tmpFs, encryptor, CryptoStreamMode.Write))
                        using(var fs = file.OpenRead()) {
                            while(true) {
                                var readlen = fs.Read(_buf, 0, _buf.Length);
                                if(readlen == 0) { break; }
                                cs.Write(_buf, 0, readlen);
                            }
                        }
                    }
                    fileLen = new FileInfo(TMP_FILE).Length;
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
                    using(var ms = new MemoryStream()) {
                        using(var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using(var fs = file.OpenRead()) {
                            while(true) {
                                var readlen = fs.Read(_buf, 0, _buf.Length);
                                if(readlen == 0) { break; }
                                cs.Write(_buf, 0, readlen);
                            }
                        }
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

        #region ReadDirectory
        private static bool ReadDirectory(Stream stream, ICryptoTransform decryptor, DirectoryInfo rootDir)
        {
            while(true) {
                if(stream.Position == stream.Length) { break; }
                // ファイル名取得
                int bufPos = 0;
                while(true) {
                    var tmp = stream.ReadByte();
                    if(tmp == -1) { return false; }     // ファイル末尾ならフォーマットエラー
                    var b = (byte)tmp;
                    if(b == 0x3A) { break; }        // 区切り文字 0x3A == ':' までがファイル名
                    _buf[bufPos++] = b;
                }
                var formattedFilePath = _encoding.GetString(_buf, 0, bufPos);

                // ハッシュ値を読む(使わないので読み飛ばす)
                if(stream.Read(_buf, 0, HASH_LEN) != HASH_LEN) { return false; }

                // 読み取るバイト長を取得
                bufPos = 0;
                while(true) {
                    var tmp = stream.ReadByte();
                    if(tmp == -1) { return false; }     // ファイル末尾ならフォーマットエラー
                    var b = (byte)tmp;
                    if(b == 0x3A) { break; }        // 区切り文字 0x3A == ':' までがファイル名
                    _buf[bufPos++] = b;
                }
                if(!long.TryParse(_encoding.GetString(_buf, 0, bufPos), out long filelen)) { return false; }

                var allLen = filelen;
                var filename = CreateDirectory(formattedFilePath, rootDir, out var dir);
                using(var fs = File.Create(Path.Combine(dir.FullName, filename)))
                using(var cs = new CryptoStream(fs, decryptor, CryptoStreamMode.Write)) {
                    while(true) {
                        if(allLen <= 0) { break; }
                        var readRequestLen = (int)(_buf.Length < allLen ? _buf.Length : allLen);
                        var readlen = stream.Read(_buf, 0, readRequestLen);
                        if(readlen != readRequestLen) { return false; }
                        allLen -= readlen;
                        cs.Write(_buf, 0, readlen);
                    }
                }
            }

            return true;
        }
        #endregion

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

        #region CreateDirectory
        private static string CreateDirectory(string formattedFilePath, DirectoryInfo root, out DirectoryInfo directory)
        {
            var path = formattedFilePath.Split('/');
            if(path.Length > 1) {
                var dir = Path.Combine(new string[1]{ root.Name }.Concat(path.Take(path.Length - 1)).ToArray());
                directory = Directory.CreateDirectory(dir);
            }
            else {
                directory = root;
            }
            return path[path.Length - 1];
        }
        #endregion
        #endregion private Method
    }
}
