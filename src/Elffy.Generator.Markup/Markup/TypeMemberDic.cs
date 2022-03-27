#nullable enable
using System.Collections.Generic;
using System;

namespace Elffy.Markup;

public readonly struct TypeMemberDic : IEquatable<TypeMemberDic>
{
    private readonly Dictionary<string, TypeInfo>? _dic;

    public bool IsNull => _dic == null;

    public static TypeMemberDic Null => default;

    public TypeMemberDic()
    {
        _dic = new Dictionary<string, TypeInfo>();
    }

    public void Add(string memberName, TypeInfo memberType)
    {
        if(_dic == null) {
            throw new InvalidOperationException();
        }
        _dic.Add(memberName, memberType);
    }

    public bool TryGetMember(string memberName, out TypeInfo typeInfo)
    {
        var dic = _dic;
        if(dic == null || dic.TryGetValue(memberName, out typeInfo) == false) {
            typeInfo = default;
            return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is TypeMemberDic dic && Equals(dic);

    public bool Equals(TypeMemberDic other) => _dic == other._dic;

    public override int GetHashCode() => _dic?.GetHashCode() ?? 0;
}
