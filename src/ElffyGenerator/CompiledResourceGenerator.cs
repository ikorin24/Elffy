#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace ElffyGenerator
{
    [Generator]
    public class CompiledResourceGenerator : ISourceGenerator
    {
        private const string AttributeText =
@"using System;

namespace Elffy
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    internal sealed class GenerateResourceFileAttribute : Attribute
    {
        public GenerateResourceFileAttribute(string resourceDir, string output, bool forceCompile = false) { }
    }
}
";
        private readonly Regex _attrRegex = new Regex(@"^(global::)?(Elffy\.)?GenerateResourceFile(Attribute)?$");

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("GenerateResourceFileAttribute", SourceText.From(AttributeText, Encoding.UTF8));

            var compilation = context.Compilation;
            var attr = compilation
                   .SyntaxTrees
                   .SelectMany(s => s.GetRoot().DescendantNodes())
                   .Where(s => s.IsKind(SyntaxKind.Attribute))
                   .OfType<AttributeSyntax>()
                   .FirstOrDefault(s => _attrRegex.IsMatch(s.Name.ToString()));

            if(attr is null) { return; }

            if(attr.ArgumentList is null) { throw new Exception("Can't be null here"); }

            var resourceDir = compilation.GetSemanticModel(attr.SyntaxTree)
                                         .GetConstantValue(attr.ArgumentList.Arguments[0].Expression)
                                         .ToString();

            var output = compilation.GetSemanticModel(attr.SyntaxTree)
                                    .GetConstantValue(attr.ArgumentList.Arguments[1].Expression)
                                    .ToString();

            var forceCompile = false;
            if(attr.ArgumentList.Arguments.Count >= 3) {
                var value = compilation.GetSemanticModel(attr.SyntaxTree)
                                       .GetConstantValue(attr.ArgumentList.Arguments[2].Expression).Value;
                if(value is not null) {
                    forceCompile = (bool)value;
                }
            }

            var globalOptions = context.AnalyzerConfigOptions.GlobalOptions;
            if(!globalOptions.TryGetValue("build_property.ProjectDir", out var projectDir)) { return; }
            if(!globalOptions.TryGetValue("build_property.TargetDir", out var targetDir)) { return; }

            var d = Path.Combine(projectDir, resourceDir);
            var o = Path.Combine(targetDir, output);

            ElffyResourceCompiler.Compiler.Compile(d, o, forceCompile);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // nop
        }
    }
}
