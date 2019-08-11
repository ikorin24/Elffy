using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ElffyResourceCompiler
{
    public static class Compiler
    {
        #region private Member
        private const string FORMAT_VERSION = "1.0";
        /// <summary>一時ファイル名</summary>
        private const string TMP_FILE = "____tmp____";
        /// <summary>一時ディレクトリ名</summary>
        private const string TMP_DIR = "____tmp_dir____";
        /// <summary>初期化ベクトルのバイト数</summary>
        private const int IV_SIZE = 16;
        /// <summary>正常解凍を確認するためのマジックワード</summary>
        private const string MAGIC_WORD = "ELFFY_RESOURCE";
        /// <summary>大きなファイルの閾値(Byte)</summary>
        private const long LARGE_FILE_SIZE = 50 * 1024 * 1024;
        /// <summary>バッファ長</summary>
        private const int BUF_LEN = 1024 * 1024;
        /// <summary>文字のエンコード</summary>
        private static readonly Encoding _encoding = Encoding.UTF8;
        /// <summary>ファイル読み込み用のバッファ</summary>
        private static byte[] _buf;
        /// <summary>ファイルのハッシュ値計算用</summary>
        private static readonly HashAlgorithm _hashFunc = new SHA256CryptoServiceProvider();
        /// <summary>ハッシュのバイト長</summary>
        private const int HASH_LEN = 32;
        /// <summary>ファイル数の書き込み部のバイトサイズ</summary>
        private const int FILE_COUNT_BYTE_COUNT = 4;
        /// <summary>ファイルサイズの書き込み部のバイトサイズ</summary>
        private const int FILE_SIZE_BYTE_COUNT = 8;
        private const string HIDDEN_ROOT = "?";
        private const string HIDDEN_ROOT_DECOMPILED = "!";
        #endregion

        #region Compile
        /// <summary>リソースのビルドを行います。</summary>
        /// <param name="targetDir">リソースディレクトリのパス</param>
        /// <param name="outputPath">出力ファイル名</param>
        public static void Compile(CompileSetting setting)
        {
            if(setting == null) { throw new ArgumentNullException(nameof(setting)); }
            var targetDir = setting.TargetDir ?? throw new ArgumentException();
            var outputPath = setting.OutputPath ?? throw new ArgumentException();
            var optionalDir = setting.OptilnalDir ?? new string[0];
            try {
                _buf = new byte[BUF_LEN];
                if(File.Exists(outputPath)) { File.Delete(outputPath); }
                Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);
                using(var fs = File.OpenWrite(outputPath)) {
                    WriteToStream(fs, FORMAT_VERSION);                  // フォーマットバージョンを出力へ書きこむ
                    WriteToStream(fs, MAGIC_WORD);                      // マジックワードを書き込む
                    int fileCount = 0;
                    fs.Position += FILE_COUNT_BYTE_COUNT;
                    var dir = new DirectoryInfo(targetDir);
                    if(Directory.Exists(dir.FullName)) {
                        WriteDirectory(dir, "", fs, ref fileCount);
                    }
                    var dirs = optionalDir.Select(x => new DirectoryInfo(x));
                    foreach(var d in dirs) {
                        if(Directory.Exists(d.FullName)) {
                            WriteDirectory(d, $"{HIDDEN_ROOT}/{d.Name}/", fs, ref fileCount);
                        }
                    }
                    fs.Position = FORMAT_VERSION.Length + MAGIC_WORD.Length;
                    fs.Write(IntToBytesLittleEndian(fileCount), 0, FILE_COUNT_BYTE_COUNT);      // ファイル数書き込み
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

        public static void DiffCompile(string directory, string outputPath)
        {
            throw new NotImplementedException();
        }

        #region Decompile
        /// <summary>リソースを解凍します</summary>
        /// <param name="inputPath">解凍するリソースのパス</param>
        /// <param name="outputDirectory">出力ディレクトリ</param>
        /// <returns></returns>
        public static bool Decompile(string inputPath, string outputDirectory)
        {
            if(outputDirectory == null) { throw new ArgumentNullException(nameof(outputDirectory)); }
            if(inputPath == null) { throw new ArgumentNullException(nameof(inputPath)); }
            if(!File.Exists(inputPath)) { throw new FileNotFoundException($"file : {inputPath}"); }
            try {
                _buf = new byte[BUF_LEN];
                if(Directory.Exists(outputDirectory)) {
                    if(Directory.Exists(TMP_DIR)) { Directory.Delete(TMP_DIR, true); }
                    Directory.Move(outputDirectory, TMP_DIR);       // 出力先ディレクトリが既に存在するなら一時退避
                }
                Directory.CreateDirectory(outputDirectory);
                
                using(var fs = File.OpenRead(inputPath)) {
                    // フォーマットバージョンの確認
                    var formatVersion = ReadFromStream(fs, 3);
                    if(formatVersion != FORMAT_VERSION) { return false; }
                    // マジックワードの確認
                    var magicWord = ReadFromStream(fs, MAGIC_WORD.Length);
                    if(magicWord != MAGIC_WORD) { return false; }
                    // ファイル数の確認
                    if(fs.Read(_buf, 0, FILE_COUNT_BYTE_COUNT) != FILE_COUNT_BYTE_COUNT) { return false; }
                    var filecount = BytesToIntLittleEndian(_buf);
                    // ディレクトリへの展開
                    ReadDirectory(fs, new DirectoryInfo(outputDirectory));
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
        #region WriteDirectory
        /// <summary>ディレクトリを指定して、その内容を出力します</summary>
        /// <param name="dir">ディレクトリ情報</param>
        /// <param name="dirName">ディレクトリ名(ファイル名の前につける相対パス)</param>
        /// <param name="stream">出力するStream</param>
        private static void WriteDirectory(DirectoryInfo dir, string dirName, Stream stream, ref int fileCount)
        {
            foreach(var file in dir.GetFiles()) {
                fileCount++;
                WriteToStream(stream, $"{dirName}{file.Name}:");       // ファイル名を出力

                // ファイルハッシュの書き込み
                using(var fs = file.OpenRead()) {
                    var hash = _hashFunc.ComputeHash(fs);
                    stream.Write(hash, 0, hash.Length);
                }

                stream.Write(LongToBytesLittleEndian(file.Length), 0, FILE_SIZE_BYTE_COUNT);    // ファイルサイズ書き込み

                // データの書き込み
                using(var fs = file.OpenRead()) {
                    while(true) {
                        var readlen = fs.Read(_buf, 0, _buf.Length);
                        if(readlen == 0) { break; }
                        stream.Write(_buf, 0, readlen);
                    }
                }
            }
            foreach(var subDir in dir.GetDirectories()) {
                WriteDirectory(subDir, $"{dirName}{subDir.Name}/", stream, ref fileCount);
            }
        }
        #endregion

        #region ReadDirectory
        private static bool ReadDirectory(Stream stream, DirectoryInfo rootDir)
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

                // ファイル長取得
                if(stream.Read(_buf, 0, FILE_SIZE_BYTE_COUNT) != FILE_SIZE_BYTE_COUNT) { return false; }
                var filelen = BytesToLongLittleEndian(_buf);

                var allLen = filelen;
                var filename = CreateDirectory(formattedFilePath, rootDir, out var dir);
                using(var fs = File.Create(Path.Combine(dir.FullName, filename))) {
                    while(true) {
                        if(allLen <= 0) { break; }
                        var readRequestLen = (int)(_buf.Length < allLen ? _buf.Length : allLen);
                        var readlen = stream.Read(_buf, 0, readRequestLen);
                        if(readlen != readRequestLen) { return false; }
                        allLen -= readlen;
                        fs.Write(_buf, 0, readlen);
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
            var path = formattedFilePath.Replace(HIDDEN_ROOT, HIDDEN_ROOT_DECOMPILED).Split('/');
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

        private static byte[] LongToBytesLittleEndian(long x) => Enumerable.Range(0, sizeof(long)).Select(i => (byte)((x >> (i * 8)) % 256)).ToArray();
        private static byte[] IntToBytesLittleEndian(int x) => Enumerable.Range(0, sizeof(int)).Select(i => (byte)((x >> (i * 8)) % 256)).ToArray();
        private static long BytesToLongLittleEndian(byte[] x) => Enumerable.Range(0, sizeof(long)).Select(i => ((long)x[i]) << (i * 8)).Sum();
        private static int BytesToIntLittleEndian(byte[] x) => Enumerable.Range(0, sizeof(int)).Select(i => x[i] << (i * 8)).Sum();
        #endregion private Method
    }
}
