#nullable enable
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using U8Xml;

namespace Elffy.Generator;

// [Generator]
public sealed class MarkupTranslationGenerator : IIncrementalGenerator
{
    private const string MarkupFileExt = ".m.xml";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sourcesProvider = context
            .AdditionalTextsProvider
            .Where(x => x.Path.EndsWith(MarkupFileExt))
            .Combine(context.CompilationProvider)
            .Select(static (x, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                var (file, compilation) = x;
                using var xml = XmlParser.ParseFile(file.Path);
                var typeInfoStore = TypeInfoStore.Create(xml, compilation, ct);
                var outputName = file.Path.Replace(MarkupFileExt, ".g.cs");

                // TODO:
                var source = MarkupTranslator.TranslateToCode(xml, "Foo", "Bar", typeInfoStore, ct);
                var sourceText = SourceText.From(source, Encoding.UTF8);
                return (OutputName: outputName, SourceText: sourceText);
            });

        context.RegisterSourceOutput(sourcesProvider, static (context, source) =>
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource(source.OutputName, source.SourceText);
        });
    }
}
