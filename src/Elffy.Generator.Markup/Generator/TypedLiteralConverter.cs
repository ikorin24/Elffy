#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System;
using Elffy.Markup;

namespace Elffy.Generator;

public static class TypedLiteralConverter
{
    public static ITypedLiteralConverter Create(INamedTypeSymbol typeSymbol, INamedTypeSymbol? typedLiteralAttr)
    {
        switch(typeSymbol.TypeKind) {
            case TypeKind.Enum: {
                var enumConverter = EnumLiteralConverter.Create(typeSymbol);
                if(typedLiteralAttr is not null && CustomLiteralConverter.TryCreate(typeSymbol, typedLiteralAttr, out var customConverter)) {
                    var converters = new ITypedLiteralConverter[] { customConverter, enumConverter };
                    return new MargedLiteralConverter(converters);
                }
                return enumConverter;
            }
            default: {
                if(typedLiteralAttr is not null && CustomLiteralConverter.TryCreate(typeSymbol, typedLiteralAttr, out var customConverter)) {
                    return customConverter;
                }
                return DoNothingLiteralConverter.Instance;
            }
        }
    }

    private sealed class DoNothingLiteralConverter : ITypedLiteralConverter
    {
        public static readonly DoNothingLiteralConverter Instance = new DoNothingLiteralConverter();

        private DoNothingLiteralConverter()
        {
        }

        public bool TryConvert(string literal, [MaybeNullWhen(false)] out string result)
        {
            result = null;
            return false;
        }
    }

    private sealed class CustomLiteralConverter : ITypedLiteralConverter
    {
        private readonly CustomLiteralRegexData[] _array;

        private CustomLiteralConverter(CustomLiteralRegexData[] array)
        {
            _array = array;
        }

        public static bool TryCreate(INamedTypeSymbol typeSymbol, INamedTypeSymbol typedLiteralAttr, [MaybeNullWhen(false)] out CustomLiteralConverter converter)
        {
            var array = EnumerateTypedLiteralData(typeSymbol, typedLiteralAttr).ToArray();
            if(array.Length == 0) {
                converter = null;
                return false;
            }
            converter = new CustomLiteralConverter(array);
            return true;

            static IEnumerable<CustomLiteralRegexData> EnumerateTypedLiteralData(INamedTypeSymbol typeSymbol, INamedTypeSymbol typedLiteralAttr)
            {
                var comparer = SymbolEqualityComparer.Default;
                foreach(var attr in typeSymbol.GetAttributes()) {
                    if(comparer.Equals(typedLiteralAttr, attr.AttributeClass) == false) { continue; }
                    if(TryGetDataFromAttr(attr, out var data)) {
                        yield return data;
                    }
                }
            }

            static bool TryGetDataFromAttr(AttributeData attr, out CustomLiteralRegexData data)
            {
                var args = attr.ConstructorArguments;
                if(args.Length != 2) { goto FAILURE; }

                var pattern = args[0].Value?.ToString();
                var replacement = args[1].Value?.ToString();
                if(pattern == null || replacement == null) { goto FAILURE; }
                try {
                    data = new CustomLiteralRegexData(new Regex(pattern), replacement);
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

    private sealed class EnumLiteralConverter : ITypedLiteralConverter
    {
        private readonly string[] _members;
        private readonly string _typeFullName;

        private EnumLiteralConverter(string typeFullName, IEnumerable<string> members)
        {
            _typeFullName = typeFullName;
            _members = members.ToArray();
        }

        public bool TryConvert(string literal, [MaybeNullWhen(false)] out string result)
        {
            foreach(var member in _members) {
                if(member == literal) {
                    result = $"global::{_typeFullName}.{member}";
                    return true;
                }
            }
            result = null;
            return false;
        }

        public static EnumLiteralConverter Create(INamedTypeSymbol enumTypeSymbol)
        {
            if(enumTypeSymbol.TypeKind != TypeKind.Enum) {
                throw new ArgumentException($"{nameof(enumTypeSymbol)} must be enum type symbol.");
            }
            var members = EnumerateMembers(enumTypeSymbol);
            return new EnumLiteralConverter(enumTypeSymbol.GetTypeName(), members);

            static IEnumerable<string> EnumerateMembers(INamedTypeSymbol enumTypeSymbol)
            {
                foreach(var m in enumTypeSymbol.GetMembers()) {
                    if(m.Kind is SymbolKind.Field && m.IsStatic) {
                        if(m.DeclaredAccessibility is Accessibility.Public) {
                            yield return m.Name;
                        }
                    }
                }
            }
        }
    }

    private sealed class MargedLiteralConverter : ITypedLiteralConverter
    {
        private readonly ITypedLiteralConverter[] _converters;

        public MargedLiteralConverter(ITypedLiteralConverter[] converters)
        {
            _converters = converters;
        }

        public bool TryConvert(string literal, [MaybeNullWhen(false)] out string result)
        {
            foreach(var converter in _converters) {
                if(converter.TryConvert(literal, out result)) {
                    return true;
                }
            }
            result = null;
            return false;
        }
    }
}

public interface ITypedLiteralConverter
{
    bool TryConvert(string literal, [MaybeNullWhen(false)] out string result);
}
