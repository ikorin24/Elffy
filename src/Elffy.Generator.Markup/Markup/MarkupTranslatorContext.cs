#nullable enable
using System.Threading;
using U8Xml;

namespace Elffy.Markup;

public sealed class MarkupTranslatorContext
{
    private readonly XmlObject _xml;
    private readonly string _builderNamespace;
    private readonly string _builderName;
    private readonly ITypeInfoStore _typeInfoStore;
    private readonly CancellationToken _ct;

    public CancellationToken CancellationToken => _ct;

    public XmlNode RootNode => _xml.Root;
    public XmlEntityTable XmlEntities => _xml.EntityTable;

    public MarkupTranslatorContext(XmlObject xml, string builderNS, string builderName, ITypeInfoStore typeInfoStore, CancellationToken ct)
    {
        _xml = xml;
        _builderNamespace = builderNS;
        _builderName = builderName;
        _typeInfoStore = typeInfoStore;
        _ct = ct;
    }
}
