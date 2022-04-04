#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Linq;
using U8Xml;
using Elffy.Markup;
using TypeInfo = Elffy.Markup.TypeInfo;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Generator;

public sealed class RoslynTypeInfoStore //: ITypeInfoStore
{
    private readonly Dictionary<string, TypeMemberDic> _typeDic;

    private RoslynTypeInfoStore(Dictionary<string, TypeMemberDic> typeDic)
    {
        _typeDic = typeDic;
    }

    public static RoslynTypeInfoStore Create(XmlObject xml, Compilation compilation, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var typedLiteralMarkupAttr = compilation.GetTypeByMetadataName("Elffy.Markup.TypedLiteralMarkupAttribute");
        var typeDic = new Dictionary<string, TypeMemberDic>();
        var state = (typeDic, typedLiteralMarkupAttr, compilation);
        CollectXmlNodeInstanceTypes(xml, state, ct, static (x, typeFullName, ct) =>
        {
            var (typeDic, typedLiteralMarkupAttr, compilation) = x;

            // TODO: generic type
            // The arg of the method should be like "System.Collections.Generic.Dictionary`2".
            // (Can not input "System.Collections.Generic.Dictionary<string, int>")
            var type = compilation.GetTypeByMetadataName(typeFullName.ToString());

            if(type == null) { return; }
            var typeName = type.ToString();
            if(typeDic.ContainsKey(typeName)) { return; }

            var typedLiteralConverter = typedLiteralMarkupAttr != null ?
                new TypedLiteralConverter(type, typedLiteralMarkupAttr) :
                TypedLiteralConverter.Empty;
            typeDic.Add(typeName, new TypeMemberDic(type));
        });
        return new RoslynTypeInfoStore(typeDic);
    }

    public bool TryGetTypeInfo(string typeName, out TypeMemberDic members)
    {
        return _typeDic.TryGetValue(typeName, out members);
    }

    private static void CollectXmlNodeInstanceTypes<T>(XmlObject xml, T state, CancellationToken ct, Action<T, TypeFullName, CancellationToken> collector)
    {
        CollectRecursively(xml.Root, state, collector, ct);

        static void CollectRecursively(XmlNode node, T state, Action<T, TypeFullName, CancellationToken> collector, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            collector.Invoke(state, node.GetTypeFullName(), ct);

            foreach(var childNode in node.Children) {
                var isPropertyNode = childNode.GetFullName().Name.Split2((byte)'.').Item2.IsEmpty == false;
                if(isPropertyNode) {
                    foreach(var valueNode in childNode.Children) {
                        CollectRecursively(valueNode, state, collector, ct);
                    }
                }
                else {
                    CollectRecursively(childNode, state, collector, ct);
                }
            }
        }
    }

    private sealed class TypedLiteralConverter
    {
        private readonly TypedLiteralData[]? _array;

        public static readonly TypedLiteralConverter Empty = new TypedLiteralConverter();

        private TypedLiteralConverter()
        {
        }

        public TypedLiteralConverter(INamedTypeSymbol typeSymbol, INamedTypeSymbol typedLiteralAttr)
        {
            _array = Create(typeSymbol, typedLiteralAttr).ToArray();
            return;

            static IEnumerable<TypedLiteralData> Create(INamedTypeSymbol type, INamedTypeSymbol typedLiteralAttr)
            {
                var comparer = SymbolEqualityComparer.Default;
                foreach(var attr in type.GetAttributes()) {
                    if(comparer.Equals(typedLiteralAttr, attr.AttributeClass) == false) { continue; }
                    if(TryGetDataFromAttr(attr, out var data)) {
                        yield return data;
                    }
                }
            }

            static bool TryGetDataFromAttr(AttributeData attr, out TypedLiteralData data)
            {
                var args = attr.ConstructorArguments;
                if(args.Length != 3) { goto FAILURE; }

                var arg0Str = args[0].Value?.ToString();
                var matchRegex = arg0Str != null ? new Regex(arg0Str) : null;
                if(matchRegex == null) { goto FAILURE; }
                var arg1Array = args[1].Values;
                var arg2Array = args[2].Values;
                if(arg1Array.Length != arg2Array.Length) { goto FAILURE; }

                var replaces = new (Regex Regex, string Replace)[arg1Array.Length];
                for(int i = 0; i < replaces.Length; i++) {
                    var rp = arg1Array[i].Value?.ToString();
                    var r = arg2Array[i].Value?.ToString();
                    if(rp == null || r == null) {
                        goto FAILURE;
                    }
                    replaces[i] = (new Regex(rp), r);
                }
                data = new TypedLiteralData(matchRegex, replaces);
                return true;

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

    private record struct TypedLiteralData(Regex MatchRegex, ReadOnlyMemory<(Regex Regex, string Replacement)> Replacements)
    {
        public bool TryConvert(string literal, [MaybeNullWhen(false)] out string result)
        {
            if(MatchRegex.Match(literal).Success) {
                result = null;
                return false;
            }
            foreach(var (regex, replacement) in Replacements.Span) {
                var matches = regex.Matches(literal);
                if(regex.IsMatch(literal) == false) {
                    continue;
                }
                result = regex.Replace(literal, replacement);
                return true;
            }
            result = null;
            return false;
        }
    }
}
