using System;
using System.IO;
using Elffy;

namespace DefinitionGenerator
{
    public class ClassCode
    {
        private const int INDENT_SIZE = 4;
        private const string INITIALIZE_METHOD = "Initialize";
        private const string IS_INIT = "_isInitialized";

        #region Property
        /// <summary>ハッシュのタイプ</summary>
        public string HashType { get; private set; }
        /// <summary>このファイルの元 xml ファイルのハッシュ</summary>
        public string FileHash { get; private set; }
        /// <summary>このクラスの名前空間</summary>
        public string NameSpace { get; private set; }
        /// <summary>このクラスの名前</summary>
        public string ClassName { get; private set; }
        /// <summary>このクラスの内容</summary>
        public DefinitionContent Content { get; private set; }
        #endregion Property

        public ClassCode(string hashType, string fileHash, string codeNamespace, string className, DefinitionContent content)
        {
            HashType = hashType ?? throw new ArgumentNullException(nameof(hashType));
            FileHash = fileHash ?? throw new ArgumentNullException(nameof(fileHash));
            ClassName = className ?? throw new ArgumentNullException(nameof(className));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            NameSpace = codeNamespace ?? throw new ArgumentNullException(nameof(codeNamespace));
        }

        #region Dump
        /// <summary>このクラスコードをファイルに出力します</summary>
        /// <param name="writer">出力用の <see cref="StreamWriter"/></param>
        public void Dump(StreamWriter writer)
        {
            if(writer == null) { throw new ArgumentNullException(nameof(writer)); }
            int indentNum = 0;
            var writeLineWithIndent = new Action<string>(line => 
            {
                var indent = new string(' ', INDENT_SIZE * indentNum);
                writer.WriteLine($"{indent}{line}");
            });
            DumpHeader(writeLineWithIndent);
            DumpUsing(writeLineWithIndent);
            DumpNamespace(writeLineWithIndent, ref indentNum);
        }
        #endregion

        #region private Method
        #region DumpHeader
        /// <summary>ファイルヘッダを出力します</summary>
        /// <param name="write">出力用関数</param>
        private void DumpHeader(Action<string> write)
        {
            var header =
$@"// ====================================
// 
// DO NOT MODIFY MANUALLY !!
// 
// This source file is auto-generated.
//
// ------------------------------------
// hash:{HashType}={FileHash}
// ====================================";
            write(header);
            write("");
        }
        #endregion

        #region DumpUsing
        /// <summary>名前空間の using を出力します</summary>
        /// <param name="write">出力用関数</param>
        private void DumpUsing(Action<string> write)
        {
            foreach(var usingNamespace in Content.Usings) {
                write($"using {usingNamespace.Namespace};");
            }
            write("");
        }
        #endregion

        #region DumpNamespace
        /// <summary>クラスの名前空間ブロック内部を出力します</summary>
        /// <param name="write">出力用関数</param>
        /// <param name="indentNum">インデント数</param>
        private void DumpNamespace(Action<string> write, ref int indentNum)
        {
            write($"namespace {NameSpace}");
            write("{");
            indentNum++;
            DumpClass(write, ref indentNum);
            indentNum--;
            write("}");
        }
        #endregion

        #region DumpClass
        /// <summary>クラスを出力します</summary>
        /// <param name="write">出力用関数</param>
        /// <param name="indentNum">インデント数</param>
        private void DumpClass(Action<string> write, ref int indentNum)
        {
            write($"partial class {ClassName} : {nameof(Elffy)}.{nameof(Definition)}");
            write("{");
            indentNum++;
            DumpVariables(write);
            DumpInitializeMethod(write, ref indentNum);
            DumpActivateMethod(write, ref indentNum);
            indentNum--;
            write("}");
        }
        #endregion

        #region DumpVariables
        /// <summary>変数定義を出力します</summary>
        /// <param name="write">出力用関数</param>
        private void DumpVariables(Action<string> write)
        {
            write($"private bool {IS_INIT};");
            foreach(var variable in Content.Variables) {
                write($"{variable.Accessability} {variable.TypeName} {variable.Name};");
            }
        }
        #endregion

        #region DumpInitializeMethod
        /// <summary>初期化関数を出力します</summary>
        /// <param name="write">出力用関数</param>
        /// <param name="indentNum">インデント数</param>
        private void DumpInitializeMethod(Action<string> write, ref int indentNum)
        {
            write("");
            write($"protected override void {INITIALIZE_METHOD}()");
            write("{");
            indentNum++;
            foreach(var variable in Content.Variables) {
                write($"{variable.Name} = new {variable.TypeName}();");
            }

            foreach(var setProp in Content.PropertySetStrings) {
                write(setProp);
            }

            //foreach(var dep in Content.Dependencies) {
            //    write($"{dep.Owner.Name}.{dep.Property} = {dep.Value.Name};");
            //}
            write($"{IS_INIT} = true;");
            indentNum--;
            write("}");
        }
        #endregion

        #region DumpActivateMethod
        /// <summary>Activateメソッドを出力します</summary>
        /// <param name="write">出力用関数</param>
        /// <param name="indentNum">インデント数</param>
        private void DumpActivateMethod(Action<string> write, ref int indentNum)
        {
            write("");
            write($"protected override void {nameof(Definition.Activate)}()");
            write("{");
            indentNum++;
            write($"if(!{IS_INIT}) {{ throw new System.InvalidOperationException(\"Instance is not initialized.\"); }}");
            foreach(var variable in Content.Variables) {
                write($"{variable.Name}.{nameof(FrameObject.Activate)}();");
            }
            indentNum--;
            write("}");
        }
        #endregion
        #endregion private Method
    }
}
