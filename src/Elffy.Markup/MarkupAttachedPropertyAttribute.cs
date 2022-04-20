#nullable enable
using System;

namespace Elffy.Markup;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class MarkupAttachedPropertyAttribute : Attribute
{
    private readonly Type[] _argTypes;

    public string Name { get; }
    public string Code { get; }
    public ReadOnlyMemory<Type> ArgTypes => _argTypes;

    public MarkupAttachedPropertyAttribute(string name, string code, Type[] argTypes)
    {
        Name = name;
        Code = code;
        _argTypes = argTypes;
    }
}
