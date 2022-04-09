#nullable enable

namespace Elffy.Markup;

public static class RegexPatterns
{
    // lang=regex
    public const string Int = @"((\+|-)?(0|[1-9]\d+))";
    // lang=regex
    public const string Float = @"((-|\+)?((((\d+\.?\d*)|(\.\d+))((E|e)(-|\+)?\d+)?)|((N|n)(A|a)(N|n))))";
}
