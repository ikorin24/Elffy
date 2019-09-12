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

namespace SceneGenerator
{
    public class CodeGenerator
    {
        #region private member
        private const string SCENE_FILE_EXT = ".xml";
        private const string GENERATED_FILE_SUFFIX = "-elffygen.cs";
        private static readonly string[] _elffyNamespaces = new string[]
        {
            nameof(Elffy),
            $"{nameof(Elffy)}.{nameof(Elffy.UI)}",
            $"{nameof(Elffy)}.{nameof(Elffy.Shape)}",
        };
        private static readonly Dictionary<string, Type> _elffyTypes;
        private readonly HashAlgorithm _hashProvider = new SHA1CryptoServiceProvider();
        private const string HASH_TYPE = "SHA1";
        #endregion

        #region constructor
        static CodeGenerator()
        {
            // Elffy の型の Dictionary をあらかじめ作っておく
            _elffyTypes = Assembly.GetAssembly(typeof(Game))
                                  .GetTypes()
                                  .Where(t => Attribute.GetCustomAttribute(t, typeof(CompilerGeneratedAttribute)) == null)
                                  .Where(t => t.IsPublic && !t.IsAbstract)
                                  .Where(t => _elffyNamespaces.Contains(t.Namespace))
                                  .ToDictionary(t => t.Name);
        }
        #endregion constructor

        public static void GenerateAll(DirectoryInfo sceneDir, DirectoryInfo outputDir)
        {
            if(sceneDir == null) { throw new ArgumentNullException(nameof(sceneDir)); }
            if(outputDir == null) { throw new ArgumentNullException(nameof(outputDir)); }

            var generator = new CodeGenerator();
            foreach(var sceneFile in sceneDir.GetFiles($"*{SCENE_FILE_EXT}")) {
                var output = Path.Combine(outputDir.FullName, $"{sceneFile.Name}{GENERATED_FILE_SUFFIX}");
                generator.Generate(sceneFile.FullName, output);
            }
        }

        #region Generate
        /// <summary>Generate c# code</summary>
        /// <param name="sceneFile">scene file name</param>
        /// <param name="outputFile">output file name</param>
        public void Generate(string sceneFile, string outputFile)
        {
            if(sceneFile == null) { throw new ArgumentNullException(nameof(sceneFile)); }
            if(outputFile == null) { throw new ArgumentNullException(nameof(outputFile)); }

            XElement xml;
            string hash;
            using(var sceneFileStream = File.OpenRead(sceneFile)) {
                hash = string.Join("", _hashProvider.ComputeHash(sceneFileStream).Select(b => b.ToString("x2")));
                if(ModifiedCheck(hash, outputFile) == false) {
                    return;
                }
                sceneFileStream.Position = 0;
                xml = XElement.Load(sceneFileStream);
            }
            var pairFileName = $"{sceneFile}.cs";
            var sceneClass = GetSceneClassName(pairFileName);
            var codeNamespace = GetCodeNamespace(pairFileName);

            #region func GetAllElements
            IEnumerable<XElement> GetAllElements(XElement root)
            {
                yield return root;
                foreach(var element in root.Elements().SelectMany(e => GetAllElements(e))) {
                    yield return element;
                }
            }
            #endregion

            CreateDirectory(outputFile);
            using(var writer = new StreamWriter(outputFile)) {
                var str0 =
$@"// ====================================
// 
// DO NOT MODIFY MANUALLY !!
// 
// This source file is auto-generated.
//
// ------------------------------------
// hash:{HASH_TYPE}={hash}
// ====================================

namespace {codeNamespace}
{{
    partial class {sceneClass} : {nameof(Elffy)}.{nameof(GameScene)}
    {{
";
                var str1 = 
$@"

        protected override void Initialize()
        {{
";
                var str2 =
$@"        }}

        public override void Activate()
        {{
";
                var str3 =
$@"        }}
    }}
}}
";
                var variables = new List<(Type type, string name)>();
                writer.Write(str0);
                writer.WriteLine($"        private bool _isInitialized;");
                foreach(var (element, i) in GetAllElements(xml).Skip(1).Select((e, i) => (e, i))) {      // Skip root element
                    var type = GetObjectType(element);
                    var variable = $"_variable{i}";
                    writer.WriteLine($"        private {type.FullName} {variable};");
                    variables.Add((type, variable));
                }
                writer.Write(str1);
                foreach(var (type, variable) in variables) {
                    writer.WriteLine($"            {variable} = new {type.FullName}();");
                }
                writer.WriteLine($"            _isInitialized = true;");
                writer.Write(str2);
                writer.WriteLine($"            if(!_isInitialized) {{ return; }}");
                foreach(var (_, variable) in variables) {
                    writer.WriteLine($"            {variable}.{nameof(FrameObject.Activate)}();");
                }
                writer.Write(str3);
            }
        }
        #endregion

        private bool ModifiedCheck(string sceneFileHash, string outputFilePath)
        {
            string currentHash = default;
            if(File.Exists(outputFilePath) == false) {
                return true;
            }
            using(var reader = new StreamReader(outputFilePath)) {
                while(!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    if(line.Contains("hash:")) {
                        currentHash = line.Split(new[] { '=' }).Last().Trim();
                        break;
                    }
                }
            }
            return sceneFileHash != currentHash;
        }

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

        #region GetObjectType
        /// <summary>xml のノードから、生成コードのクラスを取得します</summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private Type GetObjectType(XElement element)
        {
            var isElffyType = string.IsNullOrEmpty(element.Name.NamespaceName);
            Type type;
            if(isElffyType) {
                if(!_elffyTypes.TryGetValue(element.Name.LocalName, out type)) {
                    throw new SceneParseException($"Unknown type : '{element.Name.LocalName}'. NOTE: You forget namespace of type ?");
                }
            }
            else {
                var split = element.Name.NamespaceName.Split(';');
                var crlNamespace = split[0].Split(':')[1];
                var assembly = split.Length > 1 ? split[1].Split('=')[1] : null;
                type = Type.GetType($"{crlNamespace}.{element.Name.LocalName}");
                if(type == null) {
                    throw new SceneParseException($"Unknown type : '{crlNamespace}.{element.Name.LocalName}'");
                }
            }
            return type;
        }
        #endregion

        #region GetSceneClassName
        /// <summary>生成コードに出力するクラス名を取得します</summary>
        /// <param name="pairFileName">xml のペアファイルのパス</param>
        /// <returns></returns>
        private string GetSceneClassName(string pairFileName)
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
                    if(reader.EndOfStream) { throw new SceneParseException($"'{pairFileName}' is invalid."); }
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
    public class SceneParseException : Exception
    {
        public SceneParseException(string message) : base(message)
        {

        }

        public SceneParseException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
    #endregion class SceneParseException
}
