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
    private readonly ITypeInfoStore _typeInfoStore;
    private readonly IMarkupTranslationResultHolder _resultHolder;
    private readonly CancellationToken _ct;

    public CancellationToken CancellationToken => _ct;

    public XmlNode RootNode => _xml.Root;
    public XmlEntityTable XmlEntities => _xml.EntityTable;

    public ITypeInfoStore TypeInfoStore => _typeInfoStore;

    public SourceStringBuilder SourceBuilder => _sourceBuilder;

    public MarkupTranslatorContext(XmlObject xml, SourceStringBuilder sourceStringBuilder, ITypeInfoStore typeInfoStore, IMarkupTranslationResultHolder resultHolder, CancellationToken ct)
    {
        _sourceBuilder = sourceStringBuilder;
        _xml = xml;
        _typeInfoStore = typeInfoStore;
        _resultHolder = resultHolder;
        _ct = ct;
    }

    public void AddDiagnostic(Diagnostic diagnostic)
    {
        _resultHolder.AddDiagnostic(diagnostic);
    }
}
