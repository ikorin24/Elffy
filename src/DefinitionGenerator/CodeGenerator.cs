using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using Elffy;

namespace DefinitionGenerator
{
    public class CodeGenerator
    {
        #region private member
        private const string DEFINITION_FILE_EXT = ".xml";
        private const string GENERATED_FILE_SUFFIX = "-elffygen.cs";
        #endregion

        public static void GenerateAll(DirectoryInfo definitionDir, DirectoryInfo outputDir)
        {
            if(definitionDir == null) { throw new ArgumentNullException(nameof(definitionDir)); }
            if(outputDir == null) { throw new ArgumentNullException(nameof(outputDir)); }

            var generator = new CodeGenerator();

            foreach(var definitionFile in definitionDir.GetFiles($"*{DEFINITION_FILE_EXT}")) {
                var pairFileName = $"{definitionFile.FullName}.cs";
                if(!File.Exists(pairFileName)) { continue; }
                var output = Path.Combine(outputDir.FullName, $"{definitionFile.Name}{GENERATED_FILE_SUFFIX}");
                generator.Generate(definitionFile.FullName, pairFileName, output);
            }
        }

        #region Generate
        /// <summary>Generate c# code</summary>
        /// <param name="definitionFile">scene file name</param>
        /// <param name="outputFile">output file name</param>
        public void Generate(string definitionFile, string pairFile, string outputFile)
        {
            if(definitionFile == null) { throw new ArgumentNullException(nameof(definitionFile)); }
            if(outputFile == null) { throw new ArgumentNullException(nameof(outputFile)); }

            //if(!ModifiedChecker.IsModified(definitionFile, outputFile, out var hash)) {
            //    return;             // 未変更ならコード生成しない
            //}
            var hash = ModifiedChecker.GetFileHash(definitionFile);     // TODO: 戻す

            CreateDirectory(outputFile);

            var className = GetClassName(pairFile);
            var codeNamespace = GetCodeNamespace(pairFile);
            var content = DefinitionParser.Parse(definitionFile);

            using(var writer = new StreamWriter(outputFile)) {
                var classCode = new ClassCode(ModifiedChecker.HashType, hash, codeNamespace, className, content);
                classCode.Dump(writer);             // クラスのコード出力
            }
        }
        #endregion

        #region CreateDirectory
        /// <summary>出力先のディレクトリを作成します</summary>
        /// <param name="outputFile"></param>
        private void CreateDirectory(string outputFile)
        {
            var dir = new FileInfo(outputFile).Directory.FullName;
            if(Directory.Exists(dir) == false) {
                Directory.CreateDirectory(dir);
            }
        }
        #endregion

        #region GetSceneClassName
        /// <summary>生成コードに出力するクラス名を取得します</summary>
        /// <param name="pairFileName">xml のペアファイルのパス</param>
        /// <returns></returns>
        private string GetClassName(string pairFileName)
        {
            // 対象のクラス名を取得する
            // 本当は c# のソースコードをきちんとパースしてクラス名を取得するべき
            // ここではファイル名がクラス名と一致しているという前提にする

            return Path.GetFileName(pairFileName).Split(new[] { '.' })[0];
        }
        #endregion

        #region GetCodeNamespace
        /// <summary>生成コードに出力するクラスの名前空間を取得します</summary>
        /// <param name="pairFileName">xml のペアファイルのパス</param>
        /// <returns></returns>
        private string GetCodeNamespace(string pairFileName)
        {
            // ソースコードの namespace を見つける
            // ほんとは正規表現でスタイリッシュに取得したかったけど、正規表現を使いこなせていないので頑張って見つける

            // ソースコードが "きちんと" 成形されていることを前提にしているので無茶苦茶なフォーマットだとエラー出るかも

            using(var reader = new StreamReader(pairFileName)) {
                while(true) {
                    if(reader.EndOfStream) { throw new DefinitionParseException($"'{pairFileName}' is invalid."); }
                    var line = reader.ReadLine();
                    if(line.Contains("namespace")) {
                        var ns = line.Split(new[] { ' ', '{' }, StringSplitOptions.RemoveEmptyEntries)[1];
                        return ns;
                    }
                }
            }
        }
        #endregion
    }

    #region class SceneParseException
    public class DefinitionParseException : Exception
    {
        public DefinitionParseException() : base()
        {

        }

        public DefinitionParseException(string message) : base(message)
        {

        }

        public DefinitionParseException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
    #endregion class SceneParseException
}
