#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace ElffyGenerator
{
    [Generator]
    public sealed class ResourcesGenerator : ISourceGenerator
    {
        private const string AttributesDef =
@"#nullable enable
using System;
using System.Diagnostics;

namespace Elffy
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    internal sealed class DefineResourceAttribute : Attribute
    {
        public DefineResourceAttribute(string name, string outputName)
        {
        }
    }
}
";

        private static void AppendResourcesClassDef(StringBuilder sb, IEnumerable<(string Name, string FilePath)> resources)
        {
            sb.Append(
@"#nullable enable
using Elffy.Core;

namespace Elffy
{
    /// <summary>Provides resource loaders</summary>
    internal static class Resources
    {
");

            foreach(var (name, filePath) in resources) {
                sb.AppendLine(
$@"        /// <summary>Get {name} resource loader instance</summary>
        public static IResourceLoader {name} {{ get; }} = new LocalResourceLoader(""{filePath}"");");
            }

            sb.Append(@"
    }
}
");
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var sb = new StringBuilder();

            sb.Append(GeneratorUtil.GetGeneratorSigniture(typeof(ResourcesGenerator)));
            sb.Append(AttributesDef);
            context.AddSource("DefineResourceAttribute", SourceText.From(sb.ToString(), Encoding.UTF8));
            try {
                if(context.SyntaxReceiver is not SyntaxReceiver receiver) { throw new Exception("Why is the receiver null ??"); }

                sb.Clear();
                sb.Append(GeneratorUtil.GetGeneratorSigniture(typeof(ResourcesGenerator)));
                receiver.DumpSource(sb, context);
                context.AddSource("Resources", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
            catch {
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            private static readonly Regex _defineResourceRegex = new Regex(@"^(global::)?(Elffy\.)?DefineResource(Attribute)?$");

            private readonly List<AttributeSyntax> _defineResourceAttributeList = new List<AttributeSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if(syntaxNode.IsKind(SyntaxKind.Attribute) == false) { return; }
                if(syntaxNode is not AttributeSyntax attr) { return; }
                var attrName = attr.Name.ToString();

                if(_defineResourceRegex.IsMatch(attrName)) {
                    _defineResourceAttributeList.Add(attr);
                }
            }

            public void DumpSource(StringBuilder sb, GeneratorExecutionContext context)
            {
                var compilation = context.Compilation;
                var resources = _defineResourceAttributeList.Select(attr =>
                    (
                        Name: GeneratorUtil.GetAttrArgString(attr, 0, compilation),
                        FilePath: GeneratorUtil.GetAttrArgString(attr, 1, compilation)
                    ));

                AppendResourcesClassDef(sb, resources);
            }
        }
    }
}
