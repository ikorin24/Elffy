#nullable enable
using System;

namespace Elffy.Markup;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = true, Inherited = true)]
public sealed class LiteralMarkupPatternAttribute : Attribute
{
    public string Pattern { get; }
    public string Replacement { get; }

    public LiteralMarkupPatternAttribute(string pattern, string replacement)
    {
        Pattern = pattern;
        Replacement = replacement;
    }
}
