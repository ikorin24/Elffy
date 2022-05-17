#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Immutable;
using U8Xml;
using System;
using System.Text.RegularExpressions;
using System.Text;

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

    public MemberSetterList GetMemberSetterCodes(INamedTypeSymbol typeSymbol)
    {
        return MemberSetterList.Create(typeSymbol, this, _markupAttrs?.MemberSetterAttr);
    }
}

public sealed class MemberSetterList
{
    private record MemberSetterData(string MemberName, Regex Pattern, ReadOnlyMemory<Regex> ReplacePatterns, ReadOnlyMemory<string> Replaces);

    private static readonly MemberSetterList Empty = new MemberSetterList();

    private readonly INamedTypeSymbol? _ownerType;
    private readonly Dictionary<string, MemberSetterData>? _dic;
    private readonly TypeDataStore? _store;

    private MemberSetterList()
    {
    }

    private MemberSetterList(TypeDataStore store, INamedTypeSymbol ownerType, Dictionary<string, MemberSetterData> dic)
    {
        _ownerType = ownerType;
        _dic = dic;
        _store = store;
    }

    public bool TryGetSetterCode(string memberName, string literal, [MaybeNullWhen(false)] out string code, out Diagnostic? diagnostic)
    {
        // Returns false if memberName not found,
        // otherwise true even if literal does not match the pattern.

        if(_dic == null || _store == null || _ownerType == null || _ownerType == null || _dic.TryGetValue(memberName, out var data) == false) {
            code = null;
            diagnostic = null;
            return false;
        }
        if(data.Pattern.IsMatch(literal) == false) {
            code = $"// Invalid literal. memberName: {memberName}, literal: {literal}";
            diagnostic = DiagnosticHelper.InvalidLiteralForMember(literal, memberName);
            return true;
        }
        var replacePatterns = data.ReplacePatterns.Span;
        var replaces = data.Replaces.Span;
        var ownerType = _store.GetOrCreateType(_ownerType);
        code = ownerType.ReplaceMetaVariable(EvalRegexReplace(literal, replacePatterns, replaces));
        diagnostic = null;
        return true;
    }

    public static MemberSetterList Create(INamedTypeSymbol typeSymbol, TypeDataStore store, INamedTypeSymbol? memberSetterAttr)
    {
        if(memberSetterAttr == null) {
            return Empty;
        }

        var comparer = SymbolEqualityComparer.Default;
        Dictionary<string, MemberSetterData>? dic = null;

        foreach(var attr in typeSymbol.GetAttributes()) {
            if(comparer.Equals(memberSetterAttr, attr.AttributeClass) == false) { continue; }
            var args = attr.ConstructorArguments;

            if(args.Length != 4) { continue; }
            if(TryGetString(args[0], out var memberName) == false) { continue; }
            if(TryGetRegex(args[1], out var pattern) == false) { continue; }
            if(TryGetRegexPatterns(args[2].Values, out var replacePatterns) == false) { continue; }
            if(TryGetStrings(args[3].Values, out var replaces) == false) { continue; }
            if(replacePatterns.Length != replaces.Length) { continue; }

            var data = new MemberSetterData(memberName, pattern, replacePatterns, replaces);
            dic ??= new();
            if(dic.ContainsKey(memberName) == false) {
                dic.Add(memberName, data);
            }
        }
        if(dic == null) { return Empty; }
        return new MemberSetterList(store, typeSymbol, dic);
    }

    private static string EvalRegexReplace(string input, ReadOnlySpan<Regex> replacePatterns, ReadOnlySpan<string> replaces)
    {
        var matchSegments = new List<(int Start, int Length, string Replace)>();
        for(int i = 0; i < replacePatterns.Length; i++) {
            foreach(Match match in replacePatterns[i].Matches(input)) {
                var seg = (Start: match.Index, Length: match.Length, Replace: match.Result(replaces[i]));
                var addToList = true;
                foreach(var (start, len, _) in matchSegments) {
                    var isIntersect = !((seg.Start + seg.Length <= start) || (start + len <= seg.Start));
                    if(isIntersect) {
                        addToList = false;
                        break;
                    }
                }
                if(addToList) {
                    matchSegments.Add(seg);
                }
            }
        }
        matchSegments.Sort((x, y) =>
        {
            // 'Length' may be zero. (e.g. pattern "^", "$")
            // Thus, x.Start can be the same as y.Start

            var a = x.Start - y.Start;
            return a != 0 ? a : (x.Length - y.Length);  // Length may be zero. (e.g. pattern "^", "$")
        });

        var sb = new StringBuilder();
        var pos = 0;
        foreach(var seg in matchSegments) {
            sb.Append(input, pos, seg.Start - pos);
            sb.Append(seg.Replace);
            pos = seg.Start + seg.Length;
        }

        sb.Append(input, pos, input.Length - pos);
        return sb.ToString();
    }

    private static bool TryGetRegex(TypedConstant value, [MaybeNullWhen(false)] out Regex result)
    {
        if(TryGetString(value, out var str) == false) {
            result = default;
            return false;
        }
        result = new Regex(str);
        return true;
    }

    private static bool TryGetString(TypedConstant value, [MaybeNullWhen(false)] out string result)
    {
        var s = value.Value?.ToString();
        if(s == null) {
            result = null;
            return false;
        }
        result = s;
        return true;
    }

    private static bool TryGetRegexPatterns(ImmutableArray<TypedConstant> values, [MaybeNullWhen(false)] out Regex[] result)
    {
        var array = new Regex[values.Length];
        for(int i = 0; i < array.Length; i++) {
            var value = values[i].Value?.ToString();
            if(value == null) {
                result = null;
                return false;
            }
            array[i] = new Regex(value);
        }
        result = array;
        return true;
    }

    private static bool TryGetStrings(ImmutableArray<TypedConstant> values, [MaybeNullWhen(false)] out string[] result)
    {
        var array = new string[values.Length];
        for(int i = 0; i < array.Length; i++) {
            var value = values[i].Value?.ToString();
            if(value == null) {
                result = null;
                return false;
            }
            array[i] = value;
        }
        result = array;
        return true;
    }
}

public sealed class AttachedMemberList
{
    public static readonly AttachedMemberList Empty = new AttachedMemberList();

    private readonly INamedTypeSymbol? _ownerType;
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

public record MarkupAttributes
{
    public INamedTypeSymbol UseLiteralAttr { get; init; } = null!;
    public INamedTypeSymbol PatternAttr { get; init; } = null!;
    public INamedTypeSymbol MemberAttr { get; init; } = null!;
    public INamedTypeSymbol CtorAttr { get; init; } = null!;
    public INamedTypeSymbol AttachedMemberAttr { get; init; } = null!;
    public INamedTypeSymbol MemberSetterAttr { get; init; } = null!;

    private MarkupAttributes()
    {
    }

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
        return new()
        {
            UseLiteralAttr = useLiteralAttr,
            PatternAttr = patternAttr,
            MemberAttr = memberAttr,
            CtorAttr = ctorAttr,
            AttachedMemberAttr = attachedMemberAttr,
            MemberSetterAttr = memberSetter,
        };
    }
}
