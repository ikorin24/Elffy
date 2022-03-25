#nullable enable
using Microsoft.CodeAnalysis;
using System;

namespace Elffy.Generator;

public readonly struct TypeInfo : IEquatable<TypeInfo>
{
    private readonly string? _name;
    private readonly TypeInfoKind _kind;

    public string Name => _name ?? "";
    public TypeInfoKind Kind => _kind;

    public TypeInfo(INamedTypeSymbol typeSymbol)
    {
        _name = typeSymbol.Name;
        _kind = typeSymbol.TypeKind switch
        {
            TypeKind.Class => TypeInfoKind.Class,
            TypeKind.Struct => TypeInfoKind.Struct,
            TypeKind.Interface => TypeInfoKind.Interface,
            TypeKind.Enum => TypeInfoKind.Enum,
            _ => TypeInfoKind.Unknown,
        };
    }

    public override bool Equals(object? obj) => obj is TypeInfo info && Equals(info);

    public bool Equals(TypeInfo other) => _name == other._name && _kind == other._kind;

    public override int GetHashCode()
    {
        int hashCode = 349435765;
        hashCode = hashCode * -1521134295 + (_name?.GetHashCode() ?? 0);
        hashCode = hashCode * -1521134295 + _kind.GetHashCode();
        return hashCode;
    }
}
