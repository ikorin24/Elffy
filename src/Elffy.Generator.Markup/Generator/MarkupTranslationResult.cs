#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Elffy.Generator;

public sealed class MarkupTranslationResult
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
}
