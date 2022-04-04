#nullable enable
using System.Collections.Generic;
using System;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Markup;

public readonly struct TypeMemberDic : IEquatable<TypeMemberDic>
{
    private readonly INamedTypeSymbol _typeSymbol;
    private readonly Dictionary<string, TypeInfo>? _members;

    public INamedTypeSymbol OwnerTypeSymbol => _typeSymbol;
    public TypeInfo OwnerType => new TypeInfo(_typeSymbol);

    [Obsolete("Don't use default constructor.", true)]
    public TypeMemberDic() => throw new NotSupportedException("Don't use default constructor.");

    public TypeMemberDic(INamedTypeSymbol typeSymbol)
    {
        _typeSymbol = typeSymbol;
        _members = new Dictionary<string, TypeInfo>();
    }

    public void Add(string memberName, TypeInfo memberType)
    {
        if(_members == null) {
            throw new InvalidOperationException();
        }
        _members.Add(memberName, memberType);
    }

    public bool TryGetMember(string memberName, out TypeInfo typeInfo)
    {
        if(TryFindMember(_typeSymbol, memberName, out var memberType)) {
            typeInfo = new TypeInfo(memberType);
            return true;
        }
        typeInfo = default;
        return false;
    }

    private static bool TryFindMember(ITypeSymbol targetType, string memberName, [MaybeNullWhen(false)] out INamedTypeSymbol result)
    {
        var current = targetType;
        while(current != null) {
            foreach(var m in current.GetMembers(memberName)) {
                if(m.IsAssignableInstanceMember(out _, out result)) {
                    return true;
                }
            }
            current = current.BaseType;
        }
        result = null;
        return false;
    }

    public override bool Equals(object? obj) => obj is TypeMemberDic dic && Equals(dic);

    public bool Equals(TypeMemberDic other) => SymbolEqualityComparer.Default.Equals(_typeSymbol, other._typeSymbol) && (_members == other._members);

    public override int GetHashCode()
    {
        int hashCode = -1193346184;
        hashCode = hashCode * -1521134295 + SymbolEqualityComparer.Default.GetHashCode(_typeSymbol);
        hashCode = hashCode * -1521134295 + _members?.GetHashCode() ?? 0;
        return hashCode;
    }
}
