#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Elffy.Generator
{
    [Generator]
    public class EnumLikeStructGenerator : ISourceGenerator
    {
        private static readonly Regex _regexGELSA = new Regex(@"^(global::)?(Elffy\.)?GenerateEnumLikeStruct(Attribute)?$");
        private static readonly Regex _regexELVA = new Regex(@"^(global::)?(Elffy\.)?EnumLikeValue(Attribute)?$");

        private const string SourceGELSA =
@"#nullable enable
using System;
using System.Diagnostics;

namespace Elffy
{
    [Conditional(""COMPILE_TIME_ONLY"")]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    internal sealed class GenerateEnumLikeStructAttribute : Attribute
    {
        public GenerateEnumLikeStructAttribute(Type underlyingType)
        {
        }
    }
}
";
        private const string SourceELVA =
@"#nullable enable
using System;
using System.Diagnostics;

namespace Elffy
{
    [Conditional(""COMPILE_TIME_ONLY"")]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    internal sealed class EnumLikeValueAttribute : Attribute
    {
        public EnumLikeValueAttribute(string name, long value)
        {
        }

        public EnumLikeValueAttribute(string name, ulong value)
        {
        }

        public EnumLikeValueAttribute(string name, long value, string accessibility)
        {
        }

        public EnumLikeValueAttribute(string name, ulong value, string accessibility)
        {
        }

        public EnumLikeValueAttribute(string name, long value, string accessibility, string description)
        {
        }

        public EnumLikeValueAttribute(string name, ulong value, string accessibility, string description)
        {
        }
    }
}
";
        private static readonly string GeneratorSigniture = GeneratorUtil.GetGeneratorSigniture(typeof(EnumLikeStructGenerator));

        private sealed class EnumLikeMemberData
        {
            public readonly string Name;
            public readonly string Value;
            public readonly string Accessibility;
            public readonly string Description;
            public EnumLikeMemberData(string name, string value, string accessibility, string description)
            {
                Name = name;
                Value = value;
                Accessibility = accessibility;
                Description = description;
            }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("GenerateEnumLikeStructAttribute", SourceText.From(GeneratorSigniture + SourceGELSA, Encoding.UTF8));
            context.AddSource("EnumLikeValueAttribute", SourceText.From(GeneratorSigniture + SourceELVA, Encoding.UTF8));

            if(context.SyntaxReceiver is not GenerateEnumLikeStructSyntaxReciever reciever) { return; }
            foreach(var attr in reciever.GenerateEnumLikeStructAttrs) {
                try {
                    DumpGELSA(context, attr);
                }
                catch {
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new GenerateEnumLikeStructSyntaxReciever());
        }

        private static bool CheckUnderlyingType(string underlyingType)
        {
            return underlyingType is "int" or "uint" or "short" or "ushort" or "sbyte" or "byte" or "long" or "ulong";
        }

        private static void DumpGELSA(GeneratorExecutionContext context, AttributeSyntax attr)
        {
            var semantic = context.Compilation.GetSemanticModel(attr.SyntaxTree);
            var (structNS, structName) = GeneratorUtil.GetAttrTargetStructName(attr, semantic);
            var underlyingType = GeneratorUtil.GetAttrArgTypeFullName(attr, 0, semantic);
            if(CheckUnderlyingType(underlyingType) == false) { throw new Exception("Underlying type of GenerateEnumLikeStructAttribute is invalid."); }

            var members = GeneratorUtil.GetAttrTargetStructSyntax(attr)
                .AttributeLists
                .SelectMany(l => l.Attributes)
                .Where(a => _regexELVA.IsMatch(a.Name.ToString()))
                .Select(a =>
                {
                    var name = GeneratorUtil.GetAttrArgString(a, 0, semantic);
                    var value = GeneratorUtil.GetAttrArgEnumNum(a, 1, semantic);
                    var access = a.ArgumentList!.Arguments.Count >= 3 ? GeneratorUtil.GetAttrArgString(a, 2, semantic) : "public";
                    var desc = a.ArgumentList!.Arguments.Count >= 4 ? GeneratorUtil.GetAttrArgString(a, 3, semantic) : "";
                    return new EnumLikeMemberData(name, value, access, desc);
                })
                .ToArray();

            var sb = new StringBuilder();
            sb.Append(GeneratorSigniture).Append(
$@"#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace {structNS}
{{
    [DebuggerDisplay(""{{ToString(),nq}}"")]
    readonly partial struct {structName} : IEquatable<{structName}>
    {{
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly {underlyingType} _value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly {structName}[] _allValues = new {structName}[]
        {{
").AppendForeach(members, nv =>
@$"            {nv.Name},
").Append(
$@"        }};

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly {structName}[] _allPublicValues = new {structName}[]
        {{
").AppendForeach(members.Where(x => x.Accessibility == "public"), nv =>
@$"            {nv.Name},
").Append(
$@"        }};

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string[] _allNames = new string[]
        {{
").AppendForeach(members, nv =>
@$"            ""{nv.Name}"",
").Append(
$@"        }};

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string[] _allPublicNames = new string[]
        {{
").AppendForeach(members.Where(x => x.Accessibility == "public"), nv =>
@$"            ""{nv.Name}"",
").Append(
$@"        }};

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly (string Name, {structName} Value)[] _allNameValues = new (string Name, {structName} Value)[]
        {{
").AppendForeach(members, nv =>
@$"            (Name: ""{nv.Name}"", Value: {nv.Name}),
").Append(
$@"        }};

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly (string Name, {structName} Value)[] _allPublicNameValues = new (string Name, {structName} Value)[]
        {{
").AppendForeach(members.Where(x => x.Accessibility == "public"), nv =>
@$"            (Name: ""{nv.Name}"", Value: {nv.Name}),
").Append(
$@"        }};

").AppendForeach(members, nv =>
$@"        /// <summary>{nv.Description}</summary>
        {nv.Accessibility} static {structName} {nv.Name} => new {structName}(({underlyingType}){nv.Value});
").Append($@"

        private {structName}({underlyingType} value) => _value = value;

        internal static ReadOnlySpan<{structName}> AllValuesSpan() => _allValues;

        internal static IEnumerable<{structName}> AllValues() => _allValues;

        internal static ReadOnlySpan<string> AllNamesSpan() => _allNames;

        internal static IEnumerable<string> AllNames() => _allNames;

        internal static ReadOnlySpan<(string Name, {structName} Value)> AllNameValuesSpan() => _allNameValues;

        internal static IEnumerable<(string Name, {structName} Value)> AllNameValues() => _allNameValues;

        public static ReadOnlySpan<{structName}> AllPublicValuesSpan() => _allPublicValues;

        public static IEnumerable<{structName}> AllPublicValues() => _allPublicValues;

        public static ReadOnlySpan<string> AllPublicNamesSpan() => _allPublicNames;

        public static IEnumerable<string> AllPublicNames() => _allPublicNames;

        public static ReadOnlySpan<(string Name, {structName} Value)> AllPublicNameValuesSpan() => _allPublicNameValues;

        public static IEnumerable<(string Name, {structName} Value)> AllPublicNameValues() => _allPublicNameValues;

        public override bool Equals(object? obj) => obj is {structName} v && Equals(v);

        public bool Equals({structName} other) => _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==({structName} left, {structName} right) => left.Equals(right);

        public static bool operator !=({structName} left, {structName} right) => !(left == right);
");
            AppendToStringSource(sb, members, underlyingType);
            sb.Append(@"
    }
}
");
            context.AddSource(structName, SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private static void AppendToStringSource(StringBuilder sb, EnumLikeMemberData[] members, string underlyingType)
        {
            var sorted = (underlyingType == "ulong")
                ? members.OrderBy(x => ulong.Parse(x.Value)).Select(x => (x.Name, x.Value)).ToArray()
                : members.OrderBy(x => long.Parse(x.Value)).Select(x => (x.Name, x.Value)).ToArray();
            sb.AppendLine(@"
        public override string ToString()
        {
            var value = _value;");
            Foo(sorted, 3, sb);
            sb.AppendLine(@"
        }");

            static void Foo(ReadOnlySpan<(string Name, string Value)> span, int depth, StringBuilder sb)
            {
                var indent = new string(' ', depth * 4);
                if(span.Length == 0) {
                    sb.AppendLine($"{indent}return \"\";");
                    return;
                }
                if(span.Length == 1) {
                    sb.AppendLine($"{indent}return (value == {span[0].Value}) ? \"{span[0].Name}\" : \"\";");
                    return;
                }
                var m = span.Length / 2;
                var mid = span[m - 1].Value;
                sb.AppendLine(@$"{indent}if(value <= {mid}) {{");
                Foo(span.Slice(0, m), depth + 1, sb);
                sb.AppendLine($"{indent}}} else {{");
                Foo(span.Slice(m), depth + 1, sb);
                sb.AppendLine($"{indent}}}");
            }
        }

        private class GenerateEnumLikeStructSyntaxReciever : ISyntaxReceiver
        {
            private readonly List<AttributeSyntax> _listGELSA = new List<AttributeSyntax>();

            public IEnumerable<AttributeSyntax> GenerateEnumLikeStructAttrs => _listGELSA;

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if(syntaxNode.Kind() != SyntaxKind.Attribute) { return; }
                if(syntaxNode is not AttributeSyntax attr) { return; }
                var attrName = attr.Name.ToString();

                if(_regexGELSA.IsMatch(attrName)) {
                    _listGELSA.Add(attr);
                }
            }
        }
    }
}
