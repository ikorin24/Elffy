#nullable enable
using Elffy.Generator;
using Microsoft.CodeAnalysis;
using System.Threading;
using U8Xml;

namespace Elffy.Markup;

public sealed class MarkupTranslatorContext
{
    private readonly SourceStringBuilder _sourceBuilder;
    private readonly XmlObject _xml;
    private readonly TypeDataStore _typeStore;
    private readonly MarkupTranslationResult _result;
    private readonly CancellationToken _ct;

    public CancellationToken CancellationToken => _ct;

    public XmlNode RootNode => _xml.Root;
    public XmlEntityTable XmlEntities => _xml.EntityTable;

    public TypeDataStore TypeStore => _typeStore;

    public SourceStringBuilder SourceBuilder => _sourceBuilder;

    public MarkupTranslatorContext(XmlObject xml, SourceStringBuilder sourceStringBuilder, TypeDataStore typeStore, MarkupTranslationResult resultHolder, CancellationToken ct)
    {
        _sourceBuilder = sourceStringBuilder;
        _xml = xml;
        _typeStore = typeStore;
        _result = resultHolder;
        _ct = ct;
    }

    public void AddDiagnostic(Diagnostic diagnostic)
    {
        _result.AddDiagnostic(diagnostic);
    }
}
