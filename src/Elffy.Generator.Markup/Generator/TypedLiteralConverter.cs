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
    public static ITypedLiteralConverter? Create(INamedTypeSymbol typeSymbol, MarkupAttributes? attrs)
    {
        switch(typeSymbol.TypeKind) {
            case TypeKind.Enum: {
                var enumConverter = EnumLiteralConverter.Create(typeSymbol);
                if(attrs is null) {
                    return enumConverter;
                }
                else {
                    var customConverter = CustomLiteralConverter.Create(typeSymbol, attrs);
                    return new MargedLiteralConverter(enumConverter, customConverter);
                }
            }
            default: {
                if(attrs is null) {
                    return null;
                }
                else {
                    return CustomLiteralConverter.Create(typeSymbol, attrs);
                }
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
        private readonly PatternData[] _patterns;
        private readonly MemberNameData[] _members;

        private CustomLiteralConverter(PatternData[] patterns, MemberNameData[] members)
        {
            _patterns = patterns;
            _members = members;
        }

        public static CustomLiteralConverter Create(INamedTypeSymbol typeSymbol, MarkupAttributes attrs)
        {
            var patterns = EnumeratePatternsData(typeSymbol, attrs.PatternAttr).ToArray();
            var members = EnumerateMembers(typeSymbol, attrs.MemberAttr).ToArray();
            return new CustomLiteralConverter(patterns, members);

            static IEnumerable<PatternData> EnumeratePatternsData(INamedTypeSymbol typeSymbol, INamedTypeSymbol patternAttr)
            {
                var comparer = SymbolEqualityComparer.Default;
                foreach(var attr in typeSymbol.GetAttributes()) {
                    if(comparer.Equals(patternAttr, attr.AttributeClass) == false) { continue; }
                    if(TryGetDataFromAttr(attr, out var data)) {
                        yield return data;
                    }
                }
                yield break;

                static bool TryGetDataFromAttr(AttributeData attr, out PatternData data)
                {
                    var args = attr.ConstructorArguments;
                    if(args.Length != 2) { goto FAILURE; }

                    var pattern = args[0].Value?.ToString();
                    var replacement = args[1].Value?.ToString();
                    if(pattern == null || replacement == null) { goto FAILURE; }
                    try {
                        data = new PatternData(new Regex(pattern), replacement);
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

            static IEnumerable<MemberNameData> EnumerateMembers(INamedTypeSymbol typeSymbol, INamedTypeSymbol memberAttr)
            {
                var comparer = SymbolEqualityComparer.Default;
                var typeFullName = typeSymbol.GetTypeName();
                foreach(var member in typeSymbol.GetMembers()) {
                    if(!member.IsStatic || member.Kind is not SymbolKind.Property and not SymbolKind.Field) {
                        continue;
                    }
                    foreach(var attr in member.GetAttributes()) {
                        if(!comparer.Equals(attr.AttributeClass, memberAttr)) {
                            continue;
                        }
                        yield return new MemberNameData(typeFullName, member.Name);
                        break;
                    }
                }
            }
        }

        public bool TryConvert(string literal, [MaybeNullWhen(false)] out string result)
        {
            foreach(var pattern in _patterns) {
                if(pattern.TryConvert(literal, out result)) {
                    return true;
                }
            }
            foreach(var member in _members) {
                if(member.TryConvert(literal, out result)) {
                    return true;
                }
            }
            result = null;
            return false;
        }

        private record struct PatternData(Regex Regex, string Replacement)
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

        private record struct MemberNameData(string TypeFullName, string Name)
        {
            public bool TryConvert(string literal, [MaybeNullWhen(false)] out string result)
            {
                if(literal == Name) {
                    result = $"global::{TypeFullName}.{Name}";
                    return true;
                }
                result = null;
                return false;
            }
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

        public MargedLiteralConverter(params ITypedLiteralConverter[] converters)
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
