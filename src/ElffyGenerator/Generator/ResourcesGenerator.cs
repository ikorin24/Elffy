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

namespace Elffy.Generator
{
    [Generator]
    public sealed class ResourcesGenerator : ISourceGenerator
    {
        private static void AppendResourcesClassDef(StringBuilder sb, IEnumerable<(string ImportedName, string PackageFilePath)> localResources)
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

            foreach(var (importedName, packageFilePath) in localResources) {
                sb.AppendLine(
$@"        /// <summary>Get {importedName} resource loader instance</summary>
        public static IResourceLoader {importedName} {{ get; }} = ResourceProvider.LocalResource(""{packageFilePath}"");");
            }

            sb.Append(@"
    }
}
");
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var sb = new StringBuilder();
            try {
                if(context.SyntaxReceiver is not SyntaxReceiver receiver) { throw new Exception("Why is the receiver null ??"); }
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
            private static readonly Regex _defineLocalResourceRegex = new Regex(@"^(global::)?(Elffy\.)?DefineLocalResource(Attribute)?$");

            private readonly List<AttributeSyntax> _defineResourceAttributeList = new List<AttributeSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if(syntaxNode.IsKind(SyntaxKind.Attribute) == false) { return; }
                if(syntaxNode is not AttributeSyntax attr) { return; }
                var attrName = attr.Name.ToString();

                if(_defineLocalResourceRegex.IsMatch(attrName)) {
                    _defineResourceAttributeList.Add(attr);
                }
            }

            public void DumpSource(StringBuilder sb, GeneratorExecutionContext context)
            {
                var compilation = context.Compilation;
                var resources = _defineResourceAttributeList.Select(attr =>
                    (
                        ImportedName: GeneratorUtil.GetAttrArgString(attr, 0, compilation),
                        PackageFilePath: GeneratorUtil.GetAttrArgString(attr, 1, compilation)
                    ));

                AppendResourcesClassDef(sb, resources);
            }
        }
    }
}
