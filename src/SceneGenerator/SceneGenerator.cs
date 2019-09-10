using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Elffy;

namespace SceneGenerator
{
    public class CodeGenerator
    {
        #region private member
        private static readonly string[] _elffyNamespaces = new string[]
        {
            nameof(Elffy),
            $"{nameof(Elffy)}.{nameof(Elffy.UI)}",
            $"{nameof(Elffy)}.{nameof(Elffy.Shape)}",
        };
        private static readonly Dictionary<string, Type> _elffyTypes;
        private string _sceneClass;
        private string _codeNamespace;
        private int variableNum;
        private string _outputFile;
        private XElement _xml;
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

        /// <summary>Constructor of <see cref="CodeGenerator"/></summary>
        /// <param name="sceneFile">scene file name</param>
        /// <param name="outputFile">output file name</param>
        public CodeGenerator(string sceneFile, string outputFile)
        {
            if(sceneFile == null) { throw new ArgumentNullException(nameof(sceneFile)); }
            if(outputFile == null) { throw new ArgumentNullException(nameof(outputFile)); }
            using(var sceneFileStream = File.OpenRead(sceneFile)) {
                _xml = XElement.Load(sceneFileStream);
            }
            _outputFile = outputFile;
            var pairFileName = $"{sceneFile}.cs";
            _sceneClass = GetSceneClassName(pairFileName);
            _codeNamespace = GetCodeNamespace(pairFileName);
        }
        #endregion constructor

        public static void GenerateAll(DirectoryInfo sceneDir, DirectoryInfo outputDir)
        {
            if(sceneDir == null) { throw new ArgumentNullException(nameof(sceneDir)); }
            if(outputDir == null) { throw new ArgumentNullException(nameof(outputDir)); }
            foreach(var sceneFile in sceneDir.GetFiles("*.xml")) {
                var output = Path.Combine(outputDir.FullName, $"{sceneFile}.elffy-gen.cs");
                var generator = new CodeGenerator(sceneFile.FullName, output);
                generator.Generate();
            }
        }

        #region Generate
        /// <summary>Generate c# code</summary>
        public void Generate()
        {
            IEnumerable<XElement> GetAllElements(XElement root)
            {
                yield return root;
                foreach(var element in root.Elements().SelectMany(e => GetAllElements(e))) {
                    yield return element;
                }
            }
            CreateDirectory();
            using(var writer = new StreamWriter(_outputFile)) {
                var str1 = 
$@"namespace {_codeNamespace}
{{
    public partial class {_sceneClass} : {nameof(Elffy)}.{nameof(GameScene)}
    {{
        protected override void Initialize()
        {{
";
                var str2 =
$@"
        }}
    }}
}}
";
                var indent = "            ";
                writer.Write(str1);
                foreach(var element in GetAllElements(_xml).Skip(1)) {      // Skip root element
                    WriteElement(writer, element, indent);
                }
                writer.Write(str2);
            }
        }
        #endregion

        private void CreateDirectory()
        {
            var dir = new FileInfo(_outputFile).Directory.FullName;
            if(Directory.Exists(dir) == false) {
                Directory.CreateDirectory(dir);
            }
        }

        private void WriteElement(StreamWriter writer, XElement element, string indent)
        {
            var type = GetObjectType(element);
            var variable = $"variable{variableNum++}";
            writer.WriteLine($"{indent}var {variable} = new {type.FullName}();");
            writer.WriteLine($"{indent}{variable}.{nameof(FrameObject.Activate)}();");
        }

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

        private string GetSceneClassName(string pairFileName)
        {
            // 対象のクラス名を取得する
            // 本当は c# のソースコードをきちんとパースしてクラス名を取得するべき
            // ここではファイル名がクラス名と一致しているという前提にする

            return Path.GetFileName(pairFileName).Split(new[] { '.' })[0];
        }

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
