#nullable enable
using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using U8Xml;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Generator;

public sealed class TypeDataStore
{
    private readonly Dictionary<string, TypeData> _typeDic;
    private readonly MarkupAttributes? _markupAttrs;
    private readonly Compilation _compilation;

    public Compilation Compilation => _compilation;

    private TypeDataStore(Compilation compilation)
    {
        _typeDic = new Dictionary<string, TypeData>();
        _markupAttrs = MarkupAttributes.CreateOrNull(compilation);
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
        var typeSymbol = _compilation.GetTypeByMetadataNameOrSpecialType(typeName);
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

    public string? GetCtorCode(INamedTypeSymbol typeSymbol)
    {
        var markupAttrs = _markupAttrs;
        if(markupAttrs is null) {
            return null;
        }
        switch(typeSymbol.TypeKind) {
            case TypeKind.Class:
            case TypeKind.Struct: {
                return GetCtorCodePrivate(typeSymbol, markupAttrs.CtorAttr);
            }
            default: {
                return null;
            }
        }

        static string? GetCtorCodePrivate(INamedTypeSymbol typeSymbol, INamedTypeSymbol ctorAttr)
        {
            var comparer = SymbolEqualityComparer.Default;
            var current = typeSymbol;
            while(current != null) {
                foreach(var attr in current.GetAttributes()) {
                    if(comparer.Equals(ctorAttr, attr.AttributeClass) == false) { continue; }
                    var args = attr.ConstructorArguments;
                    if(args.Length == 0) { continue; }
                    var code = args[0].Value?.ToString();
                    if(code == null) { continue; }
                    return code;
                }
                current = current.BaseType;
            }
            return null;
        }
    }

    public AttachedMemberList CreateAttachedMembers(INamedTypeSymbol typeSymbol)
    {
        var markupAttrs = _markupAttrs;
        if(markupAttrs is null) {
            return AttachedMemberList.Empty;
        }
        return AttachedMemberList.Create(typeSymbol, this, markupAttrs.AttachedMemberAttr);
    }
}

public sealed class AttachedMemberList
{
    public static readonly AttachedMemberList Empty = new AttachedMemberList();

    private INamedTypeSymbol? _ownerType;
    private readonly Dictionary<string, AttachedMemberData>? _dic;
    private readonly TypeDataStore? _store;

    private AttachedMemberList()
    {
    }

    private AttachedMemberList(Dictionary<string, AttachedMemberData> dic, INamedTypeSymbol ownerType, TypeDataStore store)
    {
        _dic = dic;
        _ownerType = ownerType;
        _store = store;
    }

    public bool TryGetData(string memberName, [MaybeNullWhen(false)] out AttachedMemberData data)
    {
        var dic = _dic;
        if(dic is null) {
            data = null;
            return false;
        }
        return dic.TryGetValue(memberName, out data);
    }

    public bool TryGetCodeForSetValue(string memberName, string literal, [MaybeNullWhen(false)] out string code, out Diagnostic? diagnostic)
    {
        var dic = _dic;
        var store = _store;
        diagnostic = null;
        code = null;
        //throw new Exception($"why null ? owner: {_ownerType?.GetTypeName() ?? "null"}, memberName: {memberName}, literal: {literal}, store: {store?.ToString() ?? "null"}");
        if(dic is null || store is null || _ownerType is null) {
            return false;
        }
        if(dic.TryGetValue(memberName, out var data) == false) {
            return false;
        }
        var memberType = store.GetOrCreateType(data.MemberType);
        if(memberType.TryGetLiteralCode(literal, out var valueCode, out diagnostic) == false) {
            return false;
        }
        var ownerType = store.GetOrCreateType(_ownerType);
        // TODO: "$_"
        code = ownerType.ReplaceMetaVariable(data.Code).Replace("$_", valueCode);
        return true;
    }

    public static AttachedMemberList Create(INamedTypeSymbol typeSymbol, TypeDataStore store, INamedTypeSymbol attachedMemberAttr)
    {
        var comparer = SymbolEqualityComparer.Default;
        Dictionary<string, AttachedMemberData>? dic = null;

        foreach(var attr in typeSymbol.GetAttributes()) {
            if(comparer.Equals(attachedMemberAttr, attr.AttributeClass) == false) { continue; }
            var args = attr.ConstructorArguments;
            if(args.Length != 3) { continue; }
            var memberName = args[0].Value?.ToString();
            var code = args[1].Value?.ToString();
            var memberTypeName = args[2].Value?.ToString();
            var memberType = memberTypeName != null ? store.Compilation.GetTypeByMetadataNameOrSpecialType(memberTypeName) : null;
            if(memberName == null || code == null || memberType == null) {
                continue;
            }
            dic ??= new();
            dic[memberName] = new AttachedMemberData(typeSymbol, memberName, code, memberType);
        }
        if(dic == null) {
            return Empty;
        }
        return new AttachedMemberList(dic, typeSymbol, store);
    }
}

public sealed class AttachedMemberData
{
    public INamedTypeSymbol OwnerType { get; }
    public string MemberName { get; }
    public string Code { get; }
    public INamedTypeSymbol MemberType { get; }

    public AttachedMemberData(INamedTypeSymbol ownerType, string memberName, string code, INamedTypeSymbol memberType)
    {
        OwnerType = ownerType;
        MemberName = memberName;
        Code = code;
        MemberType = memberType;
    }
}

public record MarkupAttributes(
    INamedTypeSymbol UseLiteralAttr,
    INamedTypeSymbol PatternAttr,
    INamedTypeSymbol MemberAttr,
    INamedTypeSymbol CtorAttr,
    INamedTypeSymbol AttachedMemberAttr,
    INamedTypeSymbol MemberSetterAttr)
{
    public static MarkupAttributes? CreateOrNull(Compilation compilation)
    {
        var useLiteralAttr = compilation.GetTypeByMetadataName("Elffy.Markup.UseLiteralMarkupAttribute");
        var patternAttr = compilation.GetTypeByMetadataName("Elffy.Markup.LiteralMarkupPatternAttribute");
        var memberAttr = compilation.GetTypeByMetadataName("Elffy.Markup.LiteralMarkupMemberAttribute");
        var ctorAttr = compilation.GetTypeByMetadataName("Elffy.Markup.MarkupConstructorAttribute");
        var attachedMemberAttr = compilation.GetTypeByMetadataName("Elffy.Markup.MarkupAttachedMemberAttribute");
        var memberSetter = compilation.GetTypeByMetadataName("Elffy.Markup.MarkupMemberSetterAttribute");

        if(useLiteralAttr == null || patternAttr == null || memberAttr == null || ctorAttr == null || attachedMemberAttr == null || memberSetter == null) {
            return null;
        }

        return new MarkupAttributes(
            useLiteralAttr,
            patternAttr,
            memberAttr,
            ctorAttr,
            attachedMemberAttr,
            memberSetter);
    }
}
