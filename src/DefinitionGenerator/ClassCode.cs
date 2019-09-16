using System;
using System.Collections.Generic;
using System.IO;
using Elffy;

namespace DefinitionGenerator
{
    public class ClassCode
    {
        private const int INDENT_SIZE = 4;
        private const string INITIALIZE_METHOD = "Initialize";
        private const string IS_INIT = "_isInitialized";

        public string HashType { get; private set; }
        public string FileHash { get; private set; }
        public string NameSpace { get; private set; }
        public string ClassName { get; private set; }

        public DefinitionContent Content { get; private set; }

        public ClassCode(string hashType, string fileHash, string codeNamespace, string className, DefinitionContent content)
        {
            HashType = hashType ?? throw new ArgumentNullException(nameof(hashType));
            FileHash = fileHash ?? throw new ArgumentNullException(nameof(fileHash));
            ClassName = className ?? throw new ArgumentNullException(nameof(className));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            NameSpace = codeNamespace ?? throw new ArgumentNullException(nameof(codeNamespace));
        }

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

        private void DumpUsing(Action<string> write)
        {
            foreach(var usingNamespace in Content.Usings) {
                write($"using {usingNamespace.Namespace};");
            }
            write("");
        }

        private void DumpNamespace(Action<string> write, ref int indentNum)
        {
            write($"namespace {NameSpace}");
            write("{");
            indentNum++;
            DumpClass(write, ref indentNum);
            indentNum--;
            write("}");
        }

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

        private void DumpVariables(Action<string> write)
        {
            write($"private bool {IS_INIT};");
            foreach(var variable in Content.Variables) {
                write($"{variable.Accessability} {variable.TypeName} {variable.Name};");
            }
        }

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

            foreach(var dep in Content.Dependencies) {
                write($"{dep.Owner.Name}.{dep.Property} = {dep.Value.Name};");
            }
            write($"{IS_INIT} = true;");
            indentNum--;
            write("}");
        }

        private void DumpActivateMethod(Action<string> write, ref int indentNum)
        {
            write("");
            write($"protected override void {nameof(Definition.Activate)}()");
            write("{");
            indentNum++;
            foreach(var variable in Content.Variables) {
                write($"{variable.Name}.{nameof(FrameObject.Activate)}();");
            }
            indentNum--;
            write("}");
        }
    }
}
