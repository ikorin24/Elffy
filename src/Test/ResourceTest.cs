using System;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using ElffyResourceCompiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Elffy;

namespace Test
{
    [TestClass]
    public class ResourceTest
    {
        #region CompileTest
        /// <summary>リソースのコンパイルが出来ているかをテストします</summary>
        [TestMethod]
        public void CompileTest()
        {
            // リソースのコンパイル -> デコンパイル -> デコンパイルした全ファイルと元ファイルのハッシュが一致すればOK

            // コンパイル実行
            var source = new DirectoryInfo(Path.Combine(TestValues.FileDirectory, "ElffyResources"));
            var output = Path.Combine(".");
            var args = new string[] { source.FullName, output };
            Program.Main(args);

            // デコンパイル実行
            var decompiled = new DirectoryInfo("decompiled");
            var hashfunc = new SHA256CryptoServiceProvider();
            Compiler.Decompile(Path.Combine(output, Program.OutputFile), decompiled.FullName);

            IEnumerable<FileInfo> GetAllChildren(DirectoryInfo di) => di.GetFiles().Concat(di.GetDirectories().SelectMany(GetAllChildren));
            Uri GetDirUri(DirectoryInfo di) => new Uri($"{di.FullName}");
            Uri GetFileUri(FileInfo fi) => new Uri($"{fi.FullName}");
            bool UriEqual(Uri x, Uri y) => x.ToString().Split('/').Skip(1).SequenceEqual(y.ToString().Split('/').Skip(1));

            // デコンパイルしたファイルと元ファイルを組にして、そのハッシュの一致を確かめる
            var s = GetAllChildren(source)
                        .Select(x => (Uri: GetDirUri(source).MakeRelativeUri(GetFileUri(x)), File: x))
                        .ToList();
            var pair = GetAllChildren(decompiled)
                        .Select(x => (Uri: GetDirUri(decompiled).MakeRelativeUri(GetFileUri(x)), File: x))
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
        #endregion

        /// <summary>リソースのロードができているかをテストします。</summary>
        [TestMethod]
        public void ResourceLoadTest()
        {
            // コンパイル -> コンパイルされたリソースをロード -> もとのファイルとハッシュが一致すればOK

            // コンパイル実行
            var source = new DirectoryInfo(Path.Combine(TestValues.FileDirectory, "ElffyResources"));
            var output = Path.Combine(".");
            var args = new string[] { source.FullName, output };
            Program.Main(args);

            IEnumerable<FileInfo> GetAllChildren(DirectoryInfo di) => di.GetFiles().Concat(di.GetDirectories().SelectMany(GetAllChildren));
            Uri GetDirUri(DirectoryInfo di) => new Uri($"{di.FullName}");
            Uri GetFileUri(FileInfo fi) => new Uri($"{fi.FullName}");

            var hashfunc = new SHA256CryptoServiceProvider();

            // 元ファイルの一覧を取得
            var sourceNames = GetAllChildren(source).Select(x => GetDirUri(source).MakeRelativeUri(GetFileUri(x)))
                                                    .Select(x => string.Join("/", x.ToString().Split('/').Skip(1)))
                                                    .ToList();
            
            // リソースと元ファイルのペアを作り、そのハッシュ値の一致を確認
            Resources.Initialize();
            var pair = Resources.GetResourceNames().Select(x => (Resource: x, Original: sourceNames.Find(y => x == y))).ToArray();
            foreach(var (resouce, original) in pair) {
                byte[] hash1;
                byte[] hash2;
                using(var stream = Resources.LoadStream(resouce)) {
                    hash1 = hashfunc.ComputeHash(stream);
                }
                using(var stream = File.OpenRead(Path.Combine(source.FullName, original))) {
                    hash2 = hashfunc.ComputeHash(stream);
                }
                if(hash1.SequenceEqual(hash2) == false) {
                    throw new Exception(resouce);
                }
            }
        }
    }
}
