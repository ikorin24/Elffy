#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using Elffy.Markup;
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
        var results = context
            .AdditionalTextsProvider
            .Where(x => x.Path.EndsWith(MarkupFileExt))
            .Combine(context.CompilationProvider)
            .Select(static (x, ct) =>
            {
                var result = new MarkupTranslationResult();
                try {
                    ct.ThrowIfCancellationRequested();
                    var (file, compilation) = x;
                    var filePath = file.Path;

                    XmlObject xml;
                    try {
                        xml = XmlParser.ParseFile(filePath);
                    }
                    catch(FormatException) {
                        DiagnosticHelper.InvalidXmlFormat(filePath);
                        return result;
                    }
                    var typeInfoStore = RoslynTypeInfoStore.Create(xml, compilation, ct);
                    var outputName = filePath.Replace(MarkupFileExt, ".g.cs");

                    // TODO:
                    var source = MarkupTranslator.Translate(xml, "Foo", "Bar", typeInfoStore, ct);
                    var sourceText = SourceText.From(source, Encoding.UTF8);
                    result.SetResult(outputName, sourceText);
                }
                catch(Exception ex) when(ex is not OperationCanceledException) {
                    result.AddDiagnostic(DiagnosticHelper.GeneratorInternalException(ex));
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
                context.ReportDiagnostic(DiagnosticHelper.GeneratorInternalException(ex));
            }
        });
    }

    private sealed class MarkupTranslationResult
    {
        private string? _outputName;
        private SourceText? _sourceText;
        private List<Diagnostic>? _diagnosticList;

        public MarkupTranslationResult()
        {
        }

        public void SetResult(string outputName, SourceText sourceText)
        {
            _outputName = outputName;
            _sourceText = sourceText;
        }

        public bool TryGetResult([MaybeNullWhen(false)] out string outputName, [MaybeNullWhen(false)] out SourceText sourceText)
        {
            outputName = _outputName;
            sourceText = _sourceText;
            return sourceText != null && outputName != null;
        }

        public void AddDiagnostic(Diagnostic diagnostic)
        {
            _diagnosticList ??= new List<Diagnostic>();
            _diagnosticList.Add(diagnostic);
        }

        public void ReportDiagnosticTo(SourceProductionContext context)
        {
            if(_diagnosticList == null) { return; }
            foreach(var d in _diagnosticList) {
                context.ReportDiagnostic(d);
            }
        }
    }
}
