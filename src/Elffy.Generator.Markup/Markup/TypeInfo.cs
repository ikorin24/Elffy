#nullable enable
using Elffy.Generator;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Markup;

public readonly struct TypeInfo : IEquatable<TypeInfo>
{
    private readonly INamedTypeSymbol? _typeSymbol;
    private readonly string? _fullName;

    public static TypeInfo Null => default;
    public bool IsNull => _typeSymbol == null;

    public string Name => _fullName ?? "";

    [Obsolete("Don't use default constructo", true)]
    public TypeInfo() => throw new NotSupportedException("Don't use default constructo");

    public TypeInfo(INamedTypeSymbol typeSymbol)
    {
        _typeSymbol = typeSymbol;

        // (ex)
        // delegate:  "System.Action"
        // enum:      "System.StringSplitOptions"
        // primitive: "int"
        // class:     "System.Text.Encoding"
        // struct:    "System.TimeSpan"
        // generics:  "System.Collections.Generic.Dictionary`2"
        _fullName = typeSymbol.ToString();
    }

    public bool TryGetLiteralCode(string literal, [MaybeNullWhen(false)] out string result, [MaybeNullWhen(true)] out Diagnostic diagnostic)
    {
        var typeName = Name;
        if(_embeddedLiteralCodeGenerator.TryGetValue(typeName, out var generator)) {
            try {
                result = generator.Invoke(literal);
                diagnostic = null;
                return true;
            }
            catch(Exception ex) {
                diagnostic = DiagnosticHelper.CannotCreateValueFromLiteral(literal, typeName, ex);
                result = null;
                return false;
            }
        }
        diagnostic = DiagnosticHelper.LiteralValueNotSupported(typeName);
        result = null;
        return false;
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

    public override string ToString() => Name;

    public override bool Equals(object? obj) => obj is TypeInfo info && Equals(info);

    public bool Equals(TypeInfo other) =>
        SymbolEqualityComparer.Default.Equals(_typeSymbol, other._typeSymbol);

    public override int GetHashCode() =>
        SymbolEqualityComparer.Default.GetHashCode(_typeSymbol);


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
        ["bool"] = x => bool.Parse(x) ? "(true)" : "(false)",
    };

    private delegate string LiteralCodeGenerator(string literal);
}

internal delegate TResult RefArgFunc<T1, T2, TResult>(ref T1 arg1, T2 arg2);

internal static class ITypeSymbolExtensions
{
    public static bool IsAssignableInstanceMember(
        this ISymbol s,
        out string memberName,
        [MaybeNullWhen(false)] out INamedTypeSymbol memberType)
    {
        memberName = s.Name;
        var isAccessibleInstanceMember = (s.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal) && (!s.IsStatic);
        if(isAccessibleInstanceMember) {
            memberType = s.Kind switch
            {
                SymbolKind.Property => (s as IPropertySymbol)?.Type as INamedTypeSymbol,
                SymbolKind.Field => (s as IFieldSymbol)?.Type as INamedTypeSymbol,
                _ => null,
            };
            if(memberType == null) { return false; }
            return true;
        }
        memberType = null;
        return false;
    }
}
