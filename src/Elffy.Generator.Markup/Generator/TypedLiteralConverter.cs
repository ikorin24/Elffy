#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Generator;

public sealed class TypedLiteralConverter
{
    private readonly TypedLiteralData[]? _array;

    private static readonly TypedLiteralConverter Empty = new TypedLiteralConverter();

    private TypedLiteralConverter()
    {
    }

    private TypedLiteralConverter(IEnumerable<TypedLiteralData> array)
    {
        _array = array.ToArray();
    }

    public static TypedLiteralConverter Create(INamedTypeSymbol typeSymbol, INamedTypeSymbol? typedLiteralAttr)
    {
        if(typedLiteralAttr == null) {
            return Empty;
        }
        var array = EnumerateTypedLiteralData(typeSymbol, typedLiteralAttr);
        return new TypedLiteralConverter(array);

        static IEnumerable<TypedLiteralData> EnumerateTypedLiteralData(INamedTypeSymbol typeSymbol, INamedTypeSymbol typedLiteralAttr)
        {
            var comparer = SymbolEqualityComparer.Default;
            foreach(var attr in typeSymbol.GetAttributes()) {
                if(comparer.Equals(typedLiteralAttr, attr.AttributeClass) == false) { continue; }
                if(TryGetDataFromAttr(attr, out var data)) {
                    yield return data;
                }
            }
        }

        static bool TryGetDataFromAttr(AttributeData attr, out TypedLiteralData data)
        {
            var args = attr.ConstructorArguments;
            if(args.Length != 2) { goto FAILURE; }

            var pattern = args[0].Value?.ToString();
            var replacement = args[1].Value?.ToString();
            if(pattern == null || replacement == null) { goto FAILURE; }
            try {
                data = new TypedLiteralData(new Regex(pattern), replacement);
                return true;
            }
            catch {
                data = default;
                return false;
            }

        FAILURE:
            data = default;
            return false;
        }
    }

    public bool TryConvert(string literal, [MaybeNullWhen(false)] out string result)
    {
        var array = _array;
        if(array == null) {
            result = null;
            return false;
        }
        foreach(var data in array) {
            if(data.TryConvert(literal, out result)) {
                return true;
            }
        }
        result = null;
        return false;
    }
}
