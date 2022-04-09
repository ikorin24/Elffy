#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using U8Xml;
using Elffy.Markup;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Generator;

public sealed class TypeDataStore
{
    private readonly Dictionary<string, TypeData> _typeDic;
    private readonly MarkupAttributes? _markupAttrs;
    private readonly Compilation _compilation;

    private TypeDataStore(Compilation compilation)
    {
        _typeDic = new Dictionary<string, TypeData>();
        var useLiteralAttr = compilation.GetTypeByMetadataName("Elffy.Markup.UseLiteralMarkupAttribute");
        var patternAttr = compilation.GetTypeByMetadataName("Elffy.Markup.LiteralMarkupPatternAttribute");
        var memberAttr = compilation.GetTypeByMetadataName("Elffy.Markup.LiteralMarkupMemberAttribute");
        if(useLiteralAttr == null || patternAttr == null || memberAttr == null) {
            _markupAttrs = null;
        }
        else {
            _markupAttrs = new MarkupAttributes(useLiteralAttr, patternAttr, memberAttr);
        }
        _compilation = compilation;
    }

    public static TypeDataStore Create(Compilation compilation)
    {
        return new TypeDataStore(compilation);
    }

    public TypeData GetOrCreateType(INamedTypeSymbol typeSymbol)
    {
        if(_typeDic.TryGetValue(typeSymbol.GetTypeName(), out var type) == false) {
            type = new TypeData(typeSymbol, this);
            _typeDic.Add(type.Name, type);
        }
        return type;
    }

    public bool TryGetTypeData(string typeName, [MaybeNullWhen(false)] out TypeData type)
    {
        var typeSymbol = _compilation.GetTypeByMetadataName(typeName);
        if(typeSymbol == null) {
            type = null;
            return false;
        }
        type = GetOrCreateType(typeSymbol);
        return true;
    }

    public bool IsPropertyNode(XmlNode node, [MaybeNullWhen(false)] out TypeData propOwnerType, out RawString propName)
    {
        var (nsName, tmp) = node.GetFullName();
        (var name, propName) = tmp.Split2((byte)'.');
        var typeName = new TypeFullName(nsName, name).ToString();
        if(propName.IsEmpty || TryGetTypeData(typeName, out propOwnerType) == false) {
            propOwnerType = null;
            return false;
        }
        return true;
    }

    public ITypedLiteralConverter? CreateLiteralConverter(INamedTypeSymbol typeSymbol)
    {
        return TypedLiteralConverter.Create(typeSymbol, _markupAttrs);
    }
}

public record MarkupAttributes(INamedTypeSymbol UseLiteralAttr, INamedTypeSymbol PatternAttr, INamedTypeSymbol MemberAttr);
