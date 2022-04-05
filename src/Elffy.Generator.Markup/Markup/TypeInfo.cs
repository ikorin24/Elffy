#nullable enable
using Elffy.Generator;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Markup;

internal static class ITypeSymbolExtensions
{
    public static bool IsAssignableInstanceMember(
        this ISymbol member,
        out string memberName,
        [MaybeNullWhen(false)] out INamedTypeSymbol memberType)
    {
        memberName = member.Name;
        var isAccessibleInstanceMember = (member.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal) && (!member.IsStatic);
        if(isAccessibleInstanceMember) {
            memberType = member.Kind switch
            {
                SymbolKind.Property => (member as IPropertySymbol)?.Type as INamedTypeSymbol,
                SymbolKind.Field => (member as IFieldSymbol)?.Type as INamedTypeSymbol,
                _ => null,
            };
            if(memberType == null) { return false; }
            return true;
        }
        memberType = null;
        return false;
    }

    public static string GetTypeName(this INamedTypeSymbol typeSymbol)
    {
        // (ex)
        // delegate:  "System.Action"
        // enum:      "System.StringSplitOptions"
        // primitive: "int"
        // class:     "System.Text.Encoding"
        // struct:    "System.TimeSpan"
        // generics:  "System.Collections.Generic.Dictionary`2"
        return typeSymbol.ToString();
    }
}
