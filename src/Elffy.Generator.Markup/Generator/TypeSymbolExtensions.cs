#nullable enable
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Generator;

internal static class TypeSymbolExtensions
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

        // nullable-ref:   "string?"
        // nullable-value: "int?"
        return typeSymbol.ToString();
    }

    public static bool IsSubtypeOf(this INamedTypeSymbol self, INamedTypeSymbol type)
    {
        switch(type.TypeKind) {
            case TypeKind.Class: {
                var comparer = SymbolEqualityComparer.Default;
                var current = self;
                while(current != null) {
                    if(comparer.Equals(current, type)) {
                        return true;
                    }
                    current = current.BaseType;
                }
                return false;
            }
            case TypeKind.Interface: {
                return self.AllInterfaces.Contains(type);
            }
            default: {
                return false;
            }
        }
    }

    public static INamedTypeSymbol? GetTypeByMetadataNameOrSpecialType(this Compilation compilation, string name)
    {
        return name switch
        {
            "object" => compilation.GetSpecialType(SpecialType.System_Object),
            "bool" => compilation.GetSpecialType(SpecialType.System_Boolean),
            "char" => compilation.GetSpecialType(SpecialType.System_Char),
            "sbyte" => compilation.GetSpecialType(SpecialType.System_SByte),
            "byte" => compilation.GetSpecialType(SpecialType.System_Byte),
            "short" => compilation.GetSpecialType(SpecialType.System_Int16),
            "ushort" => compilation.GetSpecialType(SpecialType.System_UInt16),
            "int" => compilation.GetSpecialType(SpecialType.System_Int32),
            "uint" => compilation.GetSpecialType(SpecialType.System_UInt32),
            "long" => compilation.GetSpecialType(SpecialType.System_Int64),
            "ulong" => compilation.GetSpecialType(SpecialType.System_UInt64),
            "decimal" => compilation.GetSpecialType(SpecialType.System_Decimal),
            "float" => compilation.GetSpecialType(SpecialType.System_Single),
            "double" => compilation.GetSpecialType(SpecialType.System_Double),
            "string" => compilation.GetSpecialType(SpecialType.System_String),
            "nint" => compilation.GetSpecialType(SpecialType.System_IntPtr),
            "nuint" => compilation.GetSpecialType(SpecialType.System_UIntPtr),
            _ => compilation.GetTypeByMetadataName(name),
        };
    }
}
