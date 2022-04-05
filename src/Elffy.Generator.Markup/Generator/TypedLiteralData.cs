#nullable enable
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Generator;

public record struct TypedLiteralData(Regex Regex, string Replacement)
{
    public bool TryConvert(string literal, [MaybeNullWhen(false)] out string result)
    {
        var match = Regex.Match(literal);
        if(match.Success == false) {
            result = null;
            return false;
        }
        result = match.Result(Replacement);
        return true;
    }
}
