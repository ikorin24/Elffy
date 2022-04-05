#nullable enable
using System;

namespace Elffy.Markup
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = true, Inherited = true)]
    public sealed class TypedLiteralMarkupAttribute : Attribute
    {
        public string Pattern { get; }
        public string Replacement { get; }

        public TypedLiteralMarkupAttribute(string pattern, string replacement)
        {
            Pattern = pattern;
            Replacement = replacement;
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
