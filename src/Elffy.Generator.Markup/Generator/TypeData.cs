#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Elffy.Generator;

public sealed class TypeData
{
    private static readonly (string Name, string Value)[] _list = new[]
    {
        ("${obj}", "obj"),
        ("${addTask}", "context.AddTask"),
        ("${caller}", "caller"),
    };
    private const string _type = "${type}";
    private static readonly Dictionary<string, Func<string, string>> _embeddedLiteralCodeGenerator = new()
    {
        ["string"] = x => $"@\"{x}\"",
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
        // TODO: nint, nuint
        ["bool"] = x => bool.Parse(x) ? "(true)" : "(false)",
    };

    private readonly TypeDataStore _store;
    private readonly INamedTypeSymbol _symbol;
    private readonly string _fullName;
    private readonly INamedTypeSymbol? _baseTypeSymbol;
    private Dictionary<string, TypeData>? _membersCache;
    private TypeData? _baseTypeCache;
    private readonly ITypedLiteralConverter? _literalConverter;
    private readonly string _ctorCode;
    private readonly AttachedMemberList _attachedMembers;
    private readonly MemberSetterList _memberSetters;

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

        var ctorCode = store.GetCtorCode(symbol);
        _ctorCode = (ctorCode != null) ? ReplaceMetaVariable(ctorCode, _fullName) : $"new global::{_fullName}()";
        _attachedMembers = store.CreateAttachedMembers(symbol);
        _memberSetters = store.GetMemberSetterCodes(symbol);
    }

    private TypeData? GetBaseType()
    {
        var baseTypeSymbol = _baseTypeSymbol;
        if(baseTypeSymbol == null) {
            return null;
        }

        return _baseTypeCache ??= _store.GetOrCreateType(baseTypeSymbol);
    }

    public bool IsSubtypeOf(TypeData type) => _symbol.IsSubtypeOf(type.Symbol);

    public bool TryGetCodeForSetAttachedMemberValue(string memberName, string literal, [MaybeNullWhen(false)] out string code, out Diagnostic? diagnostic)
    {
        return _attachedMembers.TryGetCodeForSetValue(memberName, literal, out code, out diagnostic);
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

    public bool TryGetMemberSetterCode(string memberName, string literal, [MaybeNullWhen(false)] out string code, out Diagnostic? diagnostic)
    {
        return _memberSetters.TryGetSetterCode(memberName, literal, out code, out diagnostic);
    }

    public bool TryGetLiteralCode(string literal, [MaybeNullWhen(false)] out string code, [MaybeNullWhen(true)] out Diagnostic diagnostic)
    {
        var typeName = Name;
        var isNullable = typeName.EndsWith("?");
        // TODO:
        if(isNullable && literal == "{null}") {
            code = "null";
            diagnostic = null;
            return true;
        }
        var nonNullTypeName = isNullable ? typeName.Substring(0, typeName.Length - 1) : typeName;
        if(_embeddedLiteralCodeGenerator.TryGetValue(nonNullTypeName, out var generator)) {
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
        if(_store.TryGetTypeData("Elffy.UI.Control", out var control)) {
            if(IsSubtypeOf(control)) {
                result = $"_ = {contentCode};";
                return true;
            }
        }
        result = null;
        return false;
    }

    public string GetConstructorCode()
    {
        return _ctorCode;
    }

    public override string ToString() => _fullName;

    public string ReplaceMetaVariable(string input)
    {
        // TODO: メタ変数置換用の型を別に用意して TypeData から切り離す
        return ReplaceMetaVariable(input, _fullName);
    }

    private static string ReplaceMetaVariable(string input, string typeName)
    {
        var sb = new StringBuilder();
        ReplaceMetaVariable(input, typeName, sb, (sb, str) => sb.Append(str));
        return sb.ToString();
    }

    private static void ReplaceMetaVariable<T>(string input, string typeName, T state, Action<T, string> accumulator)
    {
        if(input.Length == 0) {
            accumulator.Invoke(state, "");
            return;
        }

        var list = _list;
        var pos = 0;
        for(int i = 0; i < input.Length; i++) {
            var current = input.AsSpan(i);
            if(current.StartsWith(_type.AsSpan())) {
                accumulator.Invoke(state, input.Substring(pos, i - pos));
                accumulator.Invoke(state, "global::");
                accumulator.Invoke(state, typeName);
                i += _type.Length;
                pos = i;
                continue;
            }

            foreach(var (name, value) in list) {
                if(current.StartsWith(name.AsSpan()) == false) { continue; }
                accumulator.Invoke(state, input.Substring(pos, i - pos));
                accumulator.Invoke(state, value);
                i += name.Length;
                pos = i;
                break;
            }
        }
        if(pos != input.Length) {
            accumulator.Invoke(state, input.Substring(pos));
        }
    }
}
