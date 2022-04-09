#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System;
using Elffy.Markup;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Generator;

public sealed class TypeData
{
    private readonly TypeDataStore _store;
    private readonly INamedTypeSymbol _symbol;
    private readonly string _fullName;
    private readonly INamedTypeSymbol? _baseTypeSymbol;
    private Dictionary<string, TypeData>? _membersCache;
    private TypeData? _baseTypeCache;
    private readonly ITypedLiteralConverter? _literalConverter;
    private static readonly Dictionary<string, Func<string, string>> _embeddedLiteralCodeGenerator = new()
    {
        ["byte"] = x => $"((byte){byte.Parse(x)})",
        ["sbyte"] = x => $"((sbyte){sbyte.Parse(x)})",
        ["ushort"] = x => $"((ushort){ushort.Parse(x)})",
        ["short"] = x => $"((short){short.Parse(x)})",
        ["uint"] = x => $"((uint){uint.Parse(x)})",
        ["int"] = x => $"((int){int.Parse(x)})",
        ["ulong"] = x => $"((ulong){ulong.Parse(x)})",
        ["long"] = x => $"((long){long.Parse(x)})",
        ["float"] = x => $"((float){float.Parse(x)})",
        ["double"] = x => $"((double){double.Parse(x)})",
        ["decimal"] = x => $"((decimal){decimal.Parse(x)})",
        ["bool"] = x => bool.Parse(x) ? "(true)" : "(false)",
    };

    public INamedTypeSymbol Symbol => _symbol;
    public string Name => _fullName;
    public bool IsValueType => _symbol.IsValueType;

    public TypeData(INamedTypeSymbol symbol, TypeDataStore store)
    {
        _symbol = symbol;
        _baseTypeSymbol = symbol.BaseType;
        _fullName = symbol.GetTypeName();
        _store = store;
        _literalConverter = store.CreateLiteralConverter(symbol);
    }

    private TypeData? GetBaseType()
    {
        var baseTypeSymbol = _baseTypeSymbol;
        if(baseTypeSymbol == null) {
            return null;
        }

        return _baseTypeCache ??= _store.GetOrCreateType(baseTypeSymbol);
    }

    public bool TryGetMember(string memberName, [MaybeNullWhen(false)] out TypeData type)
    {
        var current = this;
        while(current != null) {
            var membersCache = current._membersCache;
            if(membersCache != null) {
                if(membersCache.TryGetValue(memberName, out type)) {
                    return true;
                }
            }
            else {
                if(FindSelfMemberAndCache(current, memberName, out current._membersCache, out type)) {
                    return true;
                }
            }
            current = current.GetBaseType();
        }
        type = null;
        return false;

        static bool FindSelfMemberAndCache(TypeData self, string memberName, out Dictionary<string, TypeData> membersCache, [MaybeNullWhen(false)] out TypeData type)
        {
            var store = self._store;
            var isFound = false;
            type = null;
            var members = new Dictionary<string, TypeData>();
            foreach(var m in self._symbol.GetMembers()) {
                if(m.IsAssignableInstanceMember(out var mn, out var memberTypeSymbol) == false) {
                    continue;
                }

                var t = store.GetOrCreateType(memberTypeSymbol);
                if(mn == memberName) {
                    type = t;
                    isFound = true;
                }
                members.Add(mn, t);
            }
            membersCache = members;
            return isFound;
        }
    }

    public bool TryGetLiteralCode(string literal, [MaybeNullWhen(false)] out string code, [MaybeNullWhen(true)] out Diagnostic diagnostic)
    {
        var typeName = Name;
        if(_embeddedLiteralCodeGenerator.TryGetValue(typeName, out var generator)) {
            try {
                code = generator.Invoke(literal);
                diagnostic = null;
                return true;
            }
            catch(Exception ex) {
                diagnostic = DiagnosticHelper.InvalidLiteralWithException(literal, typeName, ex);
                code = null;
                return false;
            }
        }
        else {
            var literalConverter = _literalConverter;
            if(literalConverter is null) {
                diagnostic = DiagnosticHelper.LiteralValueNotSupported(typeName);
                code = null;
                return false;
            }
            if(literalConverter.TryConvert(literal, out code)) {
                diagnostic = null;
                return true;
            }
            else {
                diagnostic = DiagnosticHelper.InvalidLiteral(literal, typeName);
                return false;
            }
        }
    }

    public bool TryGetContentSetterCode(string contentCode, [MaybeNullWhen(false)] out string result)
    {
        // TODO:
        if(Name == "Elffy.UI.Control") {
            result = $"context.AddTask(obj.Children.Add({contentCode}));";
            return true;
        }
        result = null;
        return false;
    }

    public override string ToString() => _fullName;
}
