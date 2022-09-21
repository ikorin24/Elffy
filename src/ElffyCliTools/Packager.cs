#nullable enable
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;

namespace ElffyCliTools;

internal sealed class Packager : ConsoleAppBase
{
    private static ReadOnlySpan<byte> FormatVersion => "1000"u8;
    private static ReadOnlySpan<byte> Magic => "ELFFYRES"u8;

    private const string DefaultOutput = "res.dat";
    private const string DefaultCacheDir = "obj";

    [Command("pack-res", "Pack resources")]
    public async Task<int> PackResource(
        [Option(0, "directory to pack")] string input,
        [Option("c", "cache directory")] string cacheDir = DefaultCacheDir,
        [Option("o", "output file name")] string output = DefaultOutput,
        [Option("f", "force repack even if already packed")] bool force = false
    )
    {
        var options = PackOptions.FromArgs(input, cacheDir, output, force, Context.CancellationToken);
        LogInfo("Pack resources");
        LogInfo($"    input: {options.InputDirAbs}");
        LogInfo($"    output: {options.OutputAbs}");
        LogInfo("Start packing...");
        var sw = Stopwatch.StartNew();
        try {
            await PackOrSkip(options);
            sw.Stop();
            LogInfo($"Completed to pack resources in {sw.ElapsedMilliseconds} ms. ({options.OutputAbs})");
            return 0;
        }
        catch(Exception ex) {
            LogError(ex);
            sw.Stop();
            LogError($"Failed in packing resources in {sw.ElapsedMilliseconds} ms. ({options.OutputAbs})");
            return -1;
        }
    }

    [Command("match-res", "Match resources and packed")]
    public int MatchPackInfo(
        [Option(0, "directory to pack")] string input,
        [Option("c", "cache directory")] string cacheDir = DefaultCacheDir,
        [Option("o", "output file name")] string output = DefaultOutput
    )
    {
        var options = PackOptions.FromArgs(input, cacheDir, output, false, Context.CancellationToken);
        var sw = Stopwatch.StartNew();
        try {
            var alreadyPacked = AlreadyPacked(options);
            LogInfo(alreadyPacked ? "matched" : "not matched");
            return 0;
        }
        catch(Exception ex) {
            LogError(ex);
            sw.Stop();
            LogError($"Failed in matching resources and packed file in {sw.ElapsedMilliseconds} ms. ({options.OutputAbs})");
            return -1;
        }
    }

    [Command("watch-res", "Watch resources")]
    public int WatchResource(
        [Option(0, "directory to pack")] string input,
        [Option("c", "cache directory")] string cacheDir = DefaultCacheDir,
        [Option("o", "output file name")] string output = DefaultOutput
    )
    {
        LogError("not implemented yet.");
        return -1;
    }

    [Command("clean-res", "Clean packed")]
    public int CleanResource(
        [Option("c", "cache directory")] string cacheDir = DefaultCacheDir,
        [Option("o", "output file name")] string output = DefaultOutput
    )
    {
        var (refInfoAbs, outputAbs) = PackOptions.GetArtifactsPath(cacheDir, output);
        LogInfo("Clean resources");
        var sw = Stopwatch.StartNew();
        try {
            bool deleted = false;
            if(File.Exists(outputAbs)) {
                File.Delete(outputAbs);
                deleted = true;
                LogInfo($"    delete: {outputAbs}");
            }
            if(File.Exists(refInfoAbs)) {
                File.Delete(refInfoAbs);
                deleted = true;
                LogInfo($"    delete: {refInfoAbs}");
            }

            if(deleted == false) {
                LogInfo("There was no need to clean.");
            }

            LogInfo($"Clean packed resouces in {sw.ElapsedMilliseconds} ms. ({outputAbs})");
            return 0;
        }
        catch(Exception ex) {
            LogError(ex);
            sw.Stop();
            LogError($"Failed in cleaning packed resources in {sw.ElapsedMilliseconds} ms. ({output})");
            return -1;
        }
    }

    private async ValueTask PackOrSkip(PackOptions options)
    {
        if(options.Force == false && AlreadyPacked(options)) {
            LogInfo($"Skip packing. Resources are already packed.");
            return;
        }
        await Pack(options);
    }

    private bool AlreadyPacked(PackOptions options)
    {
        var infoPath = options.ResInfoFilePathAbs;
        if(File.Exists(infoPath) == false) { return false; }
        try {
            ResPackInfo? info;
            using(var infoFs = File.OpenRead(infoPath)) {
                info = JsonSerializer.Deserialize<ResPackInfo>(infoFs);
                if(info == null) { return false; }
            }
            if(info.FormatVersion != Encoding.UTF8.GetString(FormatVersion)) { return false; }
            if(info.Input != options.InputDirAbs) { return false; }
            var dic = info.Files.ToDictionary(f => f.Name);
            var dir = new DirectoryInfo(options.InputDirAbs);
            return RecurseFiles(dir).All(x =>
            {
                return dic.Remove(x.ResourceName, out var fInfo) &&
                    fInfo.Size == (ulong)x.File.Length &&
                    fInfo.Timestamp == (ulong)x.File.LastWriteTimeUtc.Ticks;
            }) && dic.Count == 0;
        }
        catch {
            return false;
        }
    }

    private async ValueTask Pack(PackOptions options)
    {
        // All numeric values are written in little endian.

        // [overview]
        //
        // (head)
        // HEADER
        // RES_FILE_REF ---,
        //   ...           |
        //   ...           |--> res_file_count
        // RES_FILE_REF ---'  
        // RES_DATA   ---,
        //   ...         |
        //   ...         |--> res_file_count
        // RES_DATA   ---'  
        // (end)


        // [HEADER] (16 bytes)
        //
        // name            | size (bytes)  | type     | description
        // -----------------------------------------------------------
        // magic_word      | 8             | byte[]   | 
        // format_version  | 4             | byte[]   | as utf8
        // res_file_count  | 4             | uint     | 


        // [RES_FILE_REF]
        // 
        // name            | size (bytes)  | type     | description
        // -----------------------------------------------------------
        // res_name_len    | 4             | uint     | 
        // res_name        | res_name_len  | byte[]   | as utf8
        // res_size        | 8             | ulong    | 
        // res_offset      | 8             | ulong    | byte offset from head


        // [RES_DATA]
        //
        // name            | size (bytes)  | type     | description
        // -----------------------------------------------------------
        // res_data        | res_size      | byte[]   | raw bytes data

        options.ThrowIfCancellationRequested();
        var outdir = Path.GetDirectoryName(options.OutputAbs) ?? "";
        var tmpPath = options.GetNewTemporaryFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(tmpPath) ?? "");
        Directory.CreateDirectory(outdir);
        try {
            var dir = new DirectoryInfo(options.InputDirAbs);
            if(Directory.Exists(dir.FullName) == false) {
                throw new DirectoryNotFoundException($"Could not find directory '{dir.FullName}'");
            }
            var targetFiles = RecurseFiles(dir).ToArray();
            var offsetWritePos = new (long PosToWrite, ulong Offset)[targetFiles.Length];
            var resFileInfoList = new List<ResPackFileInfo>();
            using(var fs = File.OpenWrite(tmpPath)) {
                // [HEADER]
                fs.Write(Magic);
                fs.Write(FormatVersion);
                fs.WriteUInt32LE((uint)targetFiles.Length);

                // [RES_FILE_REF]
                for(int i = 0; i < targetFiles.Length; i++) {
                    var (file, resname) = targetFiles[i];
                    var fileSize = (ulong)file.Length;
                    ulong time = (ulong)file.LastWriteTimeUtc.Ticks;
                    fs.WriteUtf8WithLength(resname);
                    fs.WriteUInt64LE(fileSize);
                    offsetWritePos[i].Item1 = fs.Position;
                    fs.Position += sizeof(ulong);
                    resFileInfoList.Add(new()
                    {
                        Name = resname,
                        Size = fileSize,
                        Timestamp = time,
                    });
                }

                // [RES_DATA]
                for(int i = 0; i < targetFiles.Length; i++) {
                    var file = targetFiles[i].File;
                    offsetWritePos[i].Item2 = (ulong)fs.Position;
                    using(var resFileStream = file.OpenRead()) {
                        resFileStream.CopyTo(fs);
                    }
                }

                foreach(var (posToWrite, offset) in offsetWritePos) {
                    fs.Position = posToWrite;
                    fs.WriteUInt64LE(offset);
                }
            }
            File.Move(tmpPath, options.OutputAbs, true);
            var outputTimestamp = (ulong)File.GetLastWriteTimeUtc(options.OutputAbs).Ticks;
            var info = new ResPackInfo(options.InputDirAbs, options.OutputAbs, outputTimestamp, Encoding.UTF8.GetString(FormatVersion), resFileInfoList);
            var infoPath = options.ResInfoFilePathAbs;
            if(File.Exists(infoPath)) {
                File.Delete(infoPath);
            }
            using(var infoFs = File.OpenWrite(infoPath)) {
                await JsonSerializer.SerializeAsync(infoFs, info, PackOptions.ResInfoJsonOptions);
            }
            LogInfo($"packed file count: {info.Files.Count}");
            return;
        }
        catch {
            if(File.Exists(tmpPath)) {
                File.Delete(tmpPath);
            }
            throw;
        }
    }

    private static IEnumerable<(FileInfo File, string ResourceName)> RecurseFiles(DirectoryInfo dir, string relative = "")
    {
        return dir.GetFiles()
            .Select(f => (File: f, ResourceName: $"{relative}{f.Name}"))
            .Concat(dir.GetDirectories().SelectMany(d => RecurseFiles(d, $"{relative}{d.Name}/")));
    }

    private record PackOptions
    {
        public static readonly JsonSerializerOptions ResInfoJsonOptions = new()
        {
            WriteIndented = true,
        };

        public required string InputDirAbs { get; init; }
        public required string OutputAbs { get; init; }
        public required string CacheDirAbs { get; init; }
        public required bool Force { get; init; }

        public required CancellationToken CancellationToken { get; init; }


        private string? _outputName;
        private string? _infoFilePathAbs;
        public string OutputName => _outputName ??= Path.GetFileName(OutputAbs);
        public string ResInfoFilePathAbs => _infoFilePathAbs ??= GetResInfoPathAbs(CacheDirAbs, OutputAbs);

        public void ThrowIfCancellationRequested() => CancellationToken.ThrowIfCancellationRequested();

        public static PackOptions FromArgs(string input, string cacheDir, string output, bool force, CancellationToken ct)
        {
            return new()
            {
                InputDirAbs = Path.GetFullPath(input),
                OutputAbs = Path.GetFullPath(output),
                CacheDirAbs = Path.GetFullPath(cacheDir),
                Force = force,
                CancellationToken = ct,
            };
        }

        public string GetNewTemporaryFilePath()
        {
            return Path.Combine(CacheDirAbs, Guid.NewGuid().ToString()[..13]);
        }

        public static (string ResInfoPathAbs, string OutputAbs) GetArtifactsPath(string cacheDir, string output)
        {
            return (
                ResInfoPathAbs: GetResInfoPathAbs(cacheDir, output),
                OutputAbs: Path.GetFullPath(output)
            );
        }

        private static string GetResInfoPathAbs(string cacheDir, string output)
        {
            return Path.Combine(Path.GetFullPath(cacheDir), $"resinfo_{Path.GetFileName(output)}.json");
        }
    }

    private void LogInfo(string message)
    {
        Context.Logger.LogInformation(message);
    }

    private void LogWarning(string message)
    {
        Context.Logger.LogWarning(message);
    }

    private void LogError(string message)
    {
        Context.Logger.LogError(message);
    }

    private void LogError(Exception ex)
    {
        Context.Logger.LogError($"{ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
    }
}

internal record ResPackInfo
(
    [property: JsonPropertyName("input")] string Input,
    [property: JsonPropertyName("output")] string Output,
    [property: JsonPropertyName("timestamp")] ulong Timestamp,
    [property: JsonPropertyName("format_version")] string FormatVersion,
    [property: JsonPropertyName("files")] List<ResPackFileInfo> Files
);

internal record struct ResPackFileInfo
(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("timestamp")] ulong Timestamp,
    [property: JsonPropertyName("size")] ulong Size
);
