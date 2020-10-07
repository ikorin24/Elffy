using System;
using System.Buffers;
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
        private const string FORMAT_VERSION = "1.0";
        /// <summary>正常解凍を確認するためのマジックワード</summary>
        private const string MAGIC_WORD = "ELFFY_RESOURCE";
        /// <summary>ファイルのハッシュ値計算用</summary>
        private static readonly HashAlgorithm _hashFunc = new SHA256CryptoServiceProvider();
        /// <summary>ハッシュのバイト長</summary>
        private const int HASH_LEN = 32;


        public static void Compile(CompileSetting setting)
        {
            if(setting == null) { throw new ArgumentNullException(nameof(setting)); }
            var outputPath = setting.OutputPath ?? throw new ArgumentException();
            var resourceDir = setting.ResourceDir;
            if(File.Exists(outputPath)) { File.Delete(outputPath); }
            Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);

            using(var fs = File.OpenWrite(outputPath)) {
                var writer = new LightBinaryWriter(fs);
                writer.WriteAsUTF8(FORMAT_VERSION);
                writer.WriteAsUTF8(MAGIC_WORD);
                var fileCountPosition = fs.Position;                // ファイル数を書き込むための場所
                int fileCount = 0;
                fs.Position += 4;   // sizeof(int)
                var dir = new DirectoryInfo(resourceDir);
                if(Directory.Exists(dir.FullName)) {
                    WriteDirectory(dir, "", writer, ref fileCount);
                }
                fs.Position = fileCountPosition;
                writer.WriteLittleEndian(fileCount);
            }
        }

        public static void DiffCompile(string directory, string outputPath)
        {
            throw new NotImplementedException();
        }

        /// <summary>リソースを解凍します</summary>
        /// <param name="inputPath">解凍するリソースのパス</param>
        /// <param name="outputDir">出力ディレクトリ</param>
        /// <returns></returns>
        public static bool Decompile(string inputPath, string outputDir)
        {
            if(outputDir == null) { throw new ArgumentNullException(nameof(outputDir)); }
            if(inputPath == null) { throw new ArgumentNullException(nameof(inputPath)); }
            if(!File.Exists(inputPath)) { throw new FileNotFoundException($"file : {inputPath}"); }

            const string TMP_DIR = "____tmp_dir____";

            try {
                if(Directory.Exists(outputDir)) {
                    if(Directory.Exists(TMP_DIR)) { Directory.Delete(TMP_DIR, true); }
                    Directory.Move(outputDir, TMP_DIR);       // 出力先ディレクトリが既に存在するなら一時退避
                }
                Directory.CreateDirectory(outputDir);
                
                using(var fs = File.OpenRead(inputPath)) {
                    var reader = new LightBinaryReader(fs);
                    if(reader.ReadString(3) != FORMAT_VERSION) { return false; }
                    if(reader.ReadString(MAGIC_WORD.Length) != MAGIC_WORD) { return false; }
                    var filecount = reader.ReadInt32();

                    ReadDirectory(reader, new DirectoryInfo(outputDir));
                }
            }
            catch(Exception) {
                if(Directory.Exists(TMP_DIR)) {
                    Directory.Move(TMP_DIR, outputDir);       // 退避させた元のディレクトリを復元
                }
                throw;
            }
            
            if(Directory.Exists(TMP_DIR)) {
                Directory.Delete(TMP_DIR, true);          // 退避させた一時ディレクトリがあるなら消す
            }
            return true;
        }

        private static void WriteDirectory(DirectoryInfo dir, string dirName, in LightBinaryWriter writer, ref int fileCount)
        {
            foreach(var file in dir.GetFiles()) {
                fileCount++;
                writer.WriteAsUTF8($"{dirName}{file.Name}:");

                using(var fs = file.OpenRead()) {
                    var hash = _hashFunc.ComputeHash(fs);
                    writer.Write(hash);
                    writer.WriteLittleEndian(file.Length);

                    fs.Position = 0;
                    var reader = new LightBinaryReader(fs);
                    reader.CopyBytesTo(writer.InnerStream, reader.Length);
                }
            }
            foreach(var subDir in dir.GetDirectories()) {
                WriteDirectory(subDir, $"{dirName}{subDir.Name}/", writer, ref fileCount);
            }
        }

        private static bool ReadDirectory(LightBinaryReader reader, DirectoryInfo rootDir)
        {
            while(true) {
                if(reader.Position == reader.Length) { break; }
                // ファイル名取得
                var formattedFilePath = reader.ReadTerminatedString(0x3a);  // 0x3a == ':'

                // ハッシュ値を読む(使わないので読み飛ばす)
                reader.Position += HASH_LEN;

                // ファイル長取得
                var filelen = reader.ReadInt64();

                var filename = CreateDirectory(formattedFilePath, rootDir, out var dir);
                using(var fs = File.Create(Path.Combine(dir.FullName, filename))) {
                    reader.CopyBytesTo(fs, filelen);
                }
            }

            return true;
        }


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
            return path[path.Length - 1];       // return filename
        }
    }
}
