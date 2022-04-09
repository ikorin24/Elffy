#nullable enable
using System;

namespace Elffy.Markup
{
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

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class UseLiteralMarkupAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class LiteralMarkupMemberAttribute : Attribute
    {
        public LiteralMarkupMemberAttribute()
        {
        }
    }

    public static class RegexPatterns
    {
        // lang=regex
        public const string Int = @"((\+|-)?(0|[1-9]\d+))";
        // lang=regex
        public const string Float = @"((-|\+)?((((\d+\.?\d*)|(\.\d+))((E|e)(-|\+)?\d+)?)|((N|n)(A|a)(N|n))))";
    }
}
