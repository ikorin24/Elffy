#nullable enable
using System;

namespace Elffy.Markup;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class MarkupConstructorAttribute : Attribute
{
    public string Code { get; }

    public MarkupConstructorAttribute(string code)
    {
        Code = code;
    }
}
