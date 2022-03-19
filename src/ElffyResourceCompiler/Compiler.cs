using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ElffyResourceCompiler
{
    public static class Compiler
    {
        private static ReadOnlySpan<byte> FormatVersion => new byte[3] { (byte)'1', (byte)'.', (byte)'0' };
        private static ReadOnlySpan<byte> MaticWord => new byte[14]
        {
            (byte)'E', (byte)'L', (byte)'F', (byte)'F', (byte)'Y', (byte)'_',
            (byte)'R', (byte)'E', (byte)'S', (byte)'O', (byte)'U', (byte)'R',(byte)'C',(byte)'E',
        };

        public static void Compile(string resourceDir, string outputPath, bool forceCompile = false)
        {
            if(resourceDir is null) { throw new ArgumentNullException(nameof(resourceDir)); }
            if(outputPath is null) { throw new ArgumentNullException(nameof(outputPath)); }

            if(!forceCompile && File.Exists(outputPath)) {
                using var stream = File.OpenRead(outputPath);
                var reader = new LightBinaryReader(stream);

                if(CheckMatch(reader, FormatVersion) == false) {
                    goto RECOMPILE;
                }
                if(CheckMatch(reader, MaticWord) == false) {
                    goto RECOMPILE;
                }
                var fileCount = reader.ReadInt32();
                var hashSum = reader.ReadInt64();

                var dir = new DirectoryInfo(resourceDir);
                if(Directory.Exists(dir.FullName)) {

                    static IEnumerable<FileInfo> GetAllSubfiles(DirectoryInfo d) => d.GetFiles().Concat(d.GetDirectories().SelectMany(GetAllSubfiles));

                    int readFileCount = 0;
                    long readHashSum = 0L;
                    foreach(var file in GetAllSubfiles(dir)) {
                        readFileCount++;
                        readHashSum += file.LastWriteTimeUtc.Ticks + file.Length;
                    }

                    if(readFileCount == fileCount && readHashSum == hashSum) {
                        return;
                    }
                    else {
                        goto RECOMPILE;
                    }
                }
            }

        RECOMPILE:
            var dirPath = new FileInfo(outputPath).DirectoryName;
            if(Directory.Exists(dirPath) == false) {
                Directory.CreateDirectory(dirPath);
            }
            string TMP_FILE = Path.Combine(Path.GetDirectoryName(outputPath)!, "____tmp____");
            if(File.Exists(outputPath)) {
                if(File.Exists(TMP_FILE)) {
                    File.Delete(TMP_FILE);
                }
                File.Move(outputPath, TMP_FILE);
            }
            try {
                CompilePrivate(resourceDir, outputPath);
                if(File.Exists(TMP_FILE)) {
                    File.Delete(TMP_FILE);
                }
            }
            catch(Exception) {
                if(File.Exists(TMP_FILE)) {
                    if(File.Exists(outputPath)) {
                        File.Delete(outputPath);
                    }
                    File.Move(TMP_FILE, outputPath);
                }
                throw;
            }
            return;
        }

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
                    if(CheckMatch(reader, FormatVersion) == false) { return false; }
                    if(CheckMatch(reader, MaticWord) == false) { return false; }
                    var filecount = reader.ReadInt32();
                    var hashSum = reader.ReadInt64();

                    ReadDirectory(reader, new DirectoryInfo(outputDir));
                }
            }
            catch(Exception) {
                if(Directory.Exists(TMP_DIR)) {
                    if(Directory.Exists(outputDir)) {
                        Directory.Delete(outputDir, true);
                    }
                    Directory.Move(TMP_DIR, outputDir);       // 退避させた元のディレクトリを復元
                }
                throw;
            }

            if(Directory.Exists(TMP_DIR)) {
                Directory.Delete(TMP_DIR, true);          // 退避させた一時ディレクトリがあるなら消す
            }
            return true;
        }

        private static bool CheckMatch(in LightBinaryReader reader, ReadOnlySpan<byte> data)
        {
            Span<byte> buffer = stackalloc byte[data.Length];
            reader.Read(buffer);
            return buffer.SequenceEqual(FormatVersion);
        }

        private static void CompilePrivate(string resourceDir, string outputPath)
        {
            using(var fs = File.OpenWrite(outputPath)) {
                var writer = new LightBinaryWriter(fs);
                writer.Write(FormatVersion);
                writer.Write(MaticWord);

                var fileCountPosition = fs.Position;                // ファイル数を書き込むための場所
                int fileCount = 0;
                fs.Position += Unsafe.SizeOf<int>();
                long hashSum = 0L;
                fs.Position += Unsafe.SizeOf<long>();

                var dir = new DirectoryInfo(resourceDir);
                if(Directory.Exists(dir.FullName)) {
                    WriteDirectory(dir, "", writer, ref fileCount, ref hashSum);
                }
                fs.Position = fileCountPosition;
                writer.WriteLittleEndian(fileCount);
                writer.WriteLittleEndian(hashSum);
            }
        }


        private static void WriteDirectory(DirectoryInfo dir, string dirName, in LightBinaryWriter writer, ref int fileCount, ref long hashSum)
        {
            foreach(var file in dir.GetFiles()) {
                fileCount++;
                var time = file.LastWriteTimeUtc.Ticks;
                var fileSize = file.Length;
                hashSum += time + fileSize;

                writer.WriteAsUTF8WithLength($"{dirName}{file.Name}");
                writer.WriteLittleEndian(time);
                writer.WriteLittleEndian(fileSize);

                using(var fs = file.OpenRead()) {
                    var reader = new LightBinaryReader(fs);
                    reader.CopyBytesTo(writer.InnerStream, reader.Length);
                }
            }
            foreach(var subDir in dir.GetDirectories()) {
                WriteDirectory(subDir, $"{dirName}{subDir.Name}/", writer, ref fileCount, ref hashSum);
            }
        }

        private static bool ReadDirectory(LightBinaryReader reader, DirectoryInfo rootDir)
        {
            while(true) {
                if(reader.Position == reader.Length) { break; }
                var formattedFilePath = reader.ReadUTF8WithLength();
                var time = reader.ReadInt64();
                var fileSize = reader.ReadInt64();

                var filename = CreateDirectory(formattedFilePath, rootDir, out var dir);
                using(var fs = File.Create(Path.Combine(dir.FullName, filename))) {
                    reader.CopyBytesTo(fs, fileSize);
                }
            }

            return true;
        }


        private static string CreateDirectory(string formattedFilePath, DirectoryInfo root, out DirectoryInfo directory)
        {
            var path = formattedFilePath.Split('/');
            if(path.Length > 1) {
                var dir = Path.Combine(new string[1] { root.Name }.Concat(path.Take(path.Length - 1)).ToArray());
                directory = Directory.CreateDirectory(dir);
            }
            else {
                directory = root;
            }
            return path[path.Length - 1];       // return filename
        }
    }
}
