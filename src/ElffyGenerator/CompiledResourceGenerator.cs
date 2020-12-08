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
using ElffyResourceCompiler;

namespace ElffyGenerator
{
    [Generator]
    public class CompiledResourceGenerator : ISourceGenerator
    {
        private const string AttributeText =
@"using System;

namespace Elffy
{
    [System.Diagnostics.Conditional(""COMPILE_TIME_ONLY"")]
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
            var attributeText = GeneratorUtil.GetGeneratorSigniture(typeof(CompiledResourceGenerator)) + AttributeText;
            context.AddSource("GenerateResourceFileAttribute", SourceText.From(attributeText, Encoding.UTF8));

            var compilation = context.Compilation;
            var attr = compilation
                   .SyntaxTrees
                   .SelectMany(s => s.GetRoot().DescendantNodes())
                   .Where(s => s.IsKind(SyntaxKind.Attribute))
                   .OfType<AttributeSyntax>()
                   .FirstOrDefault(s => _attrRegex.IsMatch(s.Name.ToString()));

            if(attr is null) { return; }

            if(attr.ArgumentList is null) { throw new Exception("Can't be null here"); }
            var argList = attr.ArgumentList;

            var resourceDir = compilation.GetSemanticModel(attr.SyntaxTree)
                                         .GetConstantValue(argList.Arguments[0].Expression)
                                         .ToString();

            var output = compilation.GetSemanticModel(attr.SyntaxTree)
                                    .GetConstantValue(argList.Arguments[1].Expression)
                                    .ToString();

            var forceCompile = false;
            if(argList.Arguments.Count >= 3) {
                var value = compilation.GetSemanticModel(attr.SyntaxTree)
                                       .GetConstantValue(argList.Arguments[2].Expression).Value;
                if(value is not null) {
                    forceCompile = (bool)value;
                }
            }

            var globalOptions = context.AnalyzerConfigOptions.GlobalOptions;
            if(!globalOptions.TryGetValue("build_property.ProjectDir", out var projectDir)) { return; }
            var dir = Path.Combine(projectDir, resourceDir);

            if(globalOptions.TryGetValue("build_property.TargetDir", out var targetDir)) {
                var o = Path.Combine(targetDir, output);
                Compiler.Compile(dir, o, forceCompile);
            }
            if(globalOptions.TryGetValue("build_property.PublishDir", out var publishDir)) {
                var o = Path.Combine(projectDir, publishDir, output);
                Compiler.Compile(dir, o, true);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // nop
        }
    }
}
