#nullable enable
using System;

namespace Elffy.Markup;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class MarkupAttachedMemberAttribute : Attribute
{
    public string Name { get; }
    public string Code { get; }
    public Type MemberType { get; }

    public MarkupAttachedMemberAttribute(string name, string code, Type memberType)
    {
        Name = name;
        Code = code;
        MemberType = memberType;
    }
}
