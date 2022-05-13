#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using U8Xml;
using System.IO;

namespace Elffy.Generator;

[Generator]
public sealed class MarkupTranslationGenerator : IIncrementalGenerator
{
    private const string MarkupFileExt = ".e.xml";
    private const string OutputSourceExt = ".g.cs";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var results = context
            .AdditionalTextsProvider
            .Where(x => x.Path.EndsWith(MarkupFileExt, StringComparison.OrdinalIgnoreCase))
            .Select((x, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return new MarkupFile(x.Path, MarkupFile.ComputeFileHash(x.Path));
            })
            .WithComparer(MarkupFileEqualityComparer.Default)
            .Combine(context.CompilationProvider)
            .Select(static (x, ct) =>
            {
                var (file, compilation) = x;
                var filePath = file.FilePath;
                var result = new MarkupTranslationResult(filePath);
                try {
                    ct.ThrowIfCancellationRequested();
                    if(compilation.Language != "C#") {
                        result.AddDiagnostic(DiagnosticHelper.LanguageNotSupported(compilation.Language));
                        return result;
                    }
                    XmlObject xml;
                    try {
                        xml = XmlParser.ParseFile(filePath);
                    }
                    catch(FormatException) {
                        DiagnosticHelper.InvalidXmlFormat(filePath);
                        return result;
                    }
                    var typeStore = TypeDataStore.Create(compilation);
                    var outputName = Path.GetFileNameWithoutExtension(filePath) + OutputSourceExt;
                    result.SetOutputName(outputName);

                    MarkupTranslator.Translate(xml, typeStore, result, ct);
                }
                catch(Exception ex) when(ex is not OperationCanceledException) {
                    result.AddDiagnostic(DiagnosticHelper.GeneratorInternalException(ex, filePath));
                }
                return result;
            });

        context.RegisterSourceOutput(results, static (context, result) =>
        {
            try {
                context.CancellationToken.ThrowIfCancellationRequested();
                result.ReportDiagnosticTo(context);
                if(result.TryGetResult(out var outputName, out var sourceText)) {
                    context.AddSource(outputName, sourceText);
                }
            }
            catch(Exception ex) when(ex is not OperationCanceledException) {
                context.ReportDiagnostic(DiagnosticHelper.GeneratorInternalException(ex, result.MarkupFilePath));
            }
        });
    }

    private sealed class MarkupFileEqualityComparer : IEqualityComparer<MarkupFile>
    {
        public static readonly MarkupFileEqualityComparer Default = new();
        private MarkupFileEqualityComparer()
        {
        }

        public bool Equals(MarkupFile x, MarkupFile y) => x.Equals(y);

        public int GetHashCode(MarkupFile obj) => obj.GetHashCode();

    }
}
