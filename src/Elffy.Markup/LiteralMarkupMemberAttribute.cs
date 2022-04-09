#nullable enable
using System;

namespace Elffy.Markup;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class LiteralMarkupMemberAttribute : Attribute
{
    public LiteralMarkupMemberAttribute()
    {
    }
}
