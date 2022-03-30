#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Elffy.Markup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
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
            .Combine(context.CompilationProvider)
            .Select(static (x, ct) =>
            {
                var result = new MarkupTranslationResult();
                try {
                    ct.ThrowIfCancellationRequested();
                    var (file, compilation) = x;
                    if(compilation.Language != "C#") {
                        result.AddDiagnostic(DiagnosticHelper.LanguageNotSupported(compilation.Language));
                        return result;
                    }
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
                    var outputName = Path.GetFileNameWithoutExtension(filePath) + OutputSourceExt;
                    result.SetOutputName(outputName);

                    MarkupTranslator.Translate(xml, typeInfoStore, result, ct);
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

    private sealed class MarkupTranslationResult : IMarkupTranslationResultHolder, IDiagnosticAccumulator
    {
        private string? _outputName;
        private SourceText? _sourceText;
        private List<Diagnostic>? _diagnosticList;

        public MarkupTranslationResult()
        {
        }

        public void SetOutputName(string outputName)
        {
            _outputName = outputName;
        }

        public void SetResult(SourceText sourceText)
        {
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

        void IDiagnosticAccumulator.AddDiagnostic(object diagnostic)
        {
            if(diagnostic is Diagnostic d) {
                AddDiagnostic(d);
            }
        }
    }
}

public interface IMarkupTranslationResultHolder
{
    void AddDiagnostic(Diagnostic diagnostic);
    void SetResult(SourceText sourceText);
}
