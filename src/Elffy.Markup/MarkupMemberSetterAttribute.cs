#nullable enable
using System;

namespace Elffy.Markup;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public class MarkupMemberSetterAttribute : Attribute
{
    private readonly string[] _replacePatterns;
    private readonly string[] _replaces;

    public string MemberName { get; }
    public string Pattern { get; }
    public ReadOnlyMemory<string> ReplacePatterns => _replacePatterns;
    public ReadOnlyMemory<string> Replaces => _replaces;

    public MarkupMemberSetterAttribute(string memberName, string pattern, string[] replacePatterns, string[] replaces)
    {
        MemberName = memberName;
        Pattern = pattern;
        _replacePatterns = replacePatterns;
        _replaces = replaces;
    }
}
