#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;
using ElffyResourceCompiler;
using Elffy;
using Xunit;

namespace UnitTest
{
    public class ResourceTest
    {
        #region CompileTest
        /// <summary>リソースのコンパイルが出来ているかをテストします</summary>
        [Fact]
        public void CompileTest()
        {
            // リソースのコンパイル -> デコンパイル -> デコンパイルした全ファイルと元ファイルのハッシュが一致すればOK

            // コンパイル実行
            var (resource, output) = Compile();

            // デコンパイル実行
            var decompiled = new DirectoryInfo("decompiled");
            var hashfunc = new SHA256CryptoServiceProvider();
            Compiler.Decompile(Path.Combine(output, Program.OutputFile), decompiled.FullName);

            IEnumerable<FileInfo> GetAllChildren(DirectoryInfo di) => di.GetFiles().Concat(di.GetDirectories().SelectMany(GetAllChildren));
            Uri GetDirUri(DirectoryInfo di) => new Uri($"{di.FullName}");
            Uri GetFileUri(FileInfo fi) => new Uri($"{fi.FullName}");
            bool UriEqual(Uri x, Uri y) => x.ToString().Split('/').Skip(1).SequenceEqual(y.ToString().Split('/').Skip(1));

            var decompiledResource = decompiled.GetDirectories().First(d => d.Name == "Resource");

            var checkTargets = new []
            {
                (Original: resource, DecompiledTarget: decompiledResource),
            };

            foreach(var (original, decompiledTarget) in checkTargets) {
                // デコンパイルしたファイルと元ファイルを組にして、そのハッシュの一致を確かめる
                var s = GetAllChildren(original)
                        .Select(x => (Uri: GetDirUri(original).MakeRelativeUri(GetFileUri(x)), File: x))
                        .ToList();
                var pair = GetAllChildren(decompiledTarget)
                            .Select(x => (Uri: GetDirUri(decompiledTarget).MakeRelativeUri(GetFileUri(x)), File: x))
                            .Select(x => (Result: x.File, Source: s.Find(y => UriEqual(x.Uri, y.Uri)).File))
                            .ToList();
                foreach(var item in pair) {
                    byte[] hash1;
                    byte[] hash2;
                    using(var stream = item.Source.OpenRead()) {
                        hash1 = hashfunc.ComputeHash(stream);
                    }
                    using(var stream = item.Result.OpenRead()) {
                        hash2 = hashfunc.ComputeHash(stream);
                    }
                    if(hash1.SequenceEqual(hash2) == false) {
                        throw new Exception(GetFileUri(item.Result).ToString());
                    }
                }
            }
        }
        #endregion

        #region CommandLineArgParserTest
        /// <summary>コマンドライン引数のParserが正常に機能していることをテストします</summary>
        [Fact]
        public void CommandLineArgParserTest()
        {
            // 正常入力テスト
            var testCase = new string[] {
                "",
                "4",
                "5 -o -t 12:30",
                "--help",
                "hoge piyo meu -i input -o output",
                "-a ikorin 24 -b hoge --hoge --time 3:34 piyo",
            };
            var answers = new CommandLineArgument[] {
                new CommandLineArgument(new string[0], new Dictionary<string, string>()),
                new CommandLineArgument(new []{ "4" }, new Dictionary<string, string>()),
                new CommandLineArgument(new []{ "5" }, new Dictionary<string, string>(){ { "-o", "" }, { "-t", "12:30"} }),
                new CommandLineArgument(new string[0], new Dictionary<string, string>(){ { "--help", "" } }),
                new CommandLineArgument(new []{ "hoge", "piyo", "meu" }, new Dictionary<string, string>(){ { "-i", "input" }, { "-o", "output"} }),
                new CommandLineArgument(new []{ "24", "piyo", }, 
                                        new Dictionary<string, string>(){ { "-a", "ikorin" }, { "-b", "hoge"}, { "--hoge", ""}, { "--time", "3:34"} }),
            };
            var parser = new CommandLineArgParser();
            foreach(var (param, answer) in testCase.Zip(answers, (test, ans) => (test, ans))) {
                var args = param.Split(new []{ ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var parsed = parser.Parse(args);
                Assert.True(parsed.Args.SequenceEqual(answer.Args));
                Assert.True(parsed.OptionalArgs.SequenceEqual(answer.OptionalArgs));
            }

            // エラー入力テスト
            var errorCase = new string?[] {
                null,
                "-o -o",
            };
            var errors = new Action<Action>[] {
                action => Assert.Throws<ArgumentNullException>(action),
                action => Assert.Throws<ArgumentException>(action),
            };
            foreach(var (param, assertError) in errorCase.Zip(errors, (test, err) => (test, err))) {
                var args = param?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)!;
                assertError(() => parser.Parse(args));
            }
        }
        #endregion

        #region ResourceLoadTest
        /// <summary>リソースのロードができているかをテストします。</summary>
        [Fact]
        public void ResourceLoadTest()
        {
            // コンパイル -> コンパイルされたリソースをロード -> もとのファイルとハッシュが一致すればOK

            // コンパイル実行
            var (resource, output) = Compile();

            IEnumerable<FileInfo> GetAllChildren(DirectoryInfo di) => di.GetFiles().Concat(di.GetDirectories().SelectMany(GetAllChildren));
            Uri GetDirUri(DirectoryInfo di) => new Uri($"{di.FullName}");
            Uri GetFileUri(FileInfo fi) => new Uri($"{fi.FullName}");

            var hashfunc = new SHA256CryptoServiceProvider();

            // 元ファイルの一覧を取得
            var sourceNames = GetAllChildren(resource).Select(x => GetDirUri(resource).MakeRelativeUri(GetFileUri(x)))
                                                      .Select(x => string.Join("/", x.ToString().Split('/').Skip(1)))
                                                      .ToList();
            
            // リソースと元ファイルのペアを作り、そのハッシュ値の一致を確認
            Resources.Initialize();
            var pair = Resources.GetResourceNames().Select(x => (Resource: x, Original: sourceNames.Find(y => x == y), Type: "Resource"))
                                .ToArray();
            foreach(var (name, original, type) in pair) {
                byte[] hash1;
                byte[] hash2;
                var stream1 = (type == "Resource") ? Resources.GetStream(name) : throw new Exception();
                var stream2 = (type == "Resource") ? File.OpenRead(Path.Combine(resource.FullName, original)) : throw new Exception();
                using(stream1) {
                    hash1 = hashfunc.ComputeHash(stream1);
                }
                using(stream2) {
                    hash2 = hashfunc.ComputeHash(stream2);
                }
                if(hash1.SequenceEqual(hash2) == false) {
                    throw new Exception(name);
                }
            }
        }
        #endregion

        private (DirectoryInfo resource, string output) Compile()
        {
            // コンパイル実行
            var resource = new DirectoryInfo(Path.Combine(TestValues.FileDirectory, "ElffyResources"));
            var output = Path.Combine(".");
            var args = new[] { "-r", resource.FullName, output };
            Program.Main(args);
            return (resource, output);
        }
    }
}
