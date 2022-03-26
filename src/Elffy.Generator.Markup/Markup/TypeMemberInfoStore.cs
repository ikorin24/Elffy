#nullable enable
using System.Collections.Generic;

namespace Elffy.Markup;

public readonly struct TypeMemberInfoStore
{
    private readonly Dictionary<string, TypeInfo>? _members;

    public TypeMemberInfoStore(Dictionary<string, TypeInfo> members)
    {
        _members = members;
    }

    public bool TryGetMember(string memberName, out TypeInfo typeInfo)
    {
        var members = _members;
        if(members == null || members.TryGetValue(memberName, out typeInfo) == false) {
            typeInfo = default;
            return false;
        }
        return true;
    }
}
