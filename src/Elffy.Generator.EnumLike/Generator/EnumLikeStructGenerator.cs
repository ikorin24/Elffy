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
@"using System;
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
@"using System;
using System.Diagnostics;

namespace Elffy
{
    [Conditional(""COMPILE_TIME_ONLY"")]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    internal sealed class EnumLikeValueAttribute : Attribute
    {
        public EnumLikeValueAttribute(string name, int value, string description = """")
        {
        }
    }
}
";
        private static readonly string GeneratorSigniture = GeneratorUtil.GetGeneratorSigniture(typeof(EnumLikeStructGenerator));

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

            var nameValuePairs = GeneratorUtil.GetAttrTargetStructSyntax(attr)
                .AttributeLists
                .SelectMany(l => l.Attributes)
                .Where(a => _regexELVA.IsMatch(a.Name.ToString()))
                .Select(a => (Name: GeneratorUtil.GetAttrArgString(a, 0, semantic),
                              Value: GeneratorUtil.GetAttrArgEnumNum(a, 1, semantic),
                              Description: a.ArgumentList!.Arguments.Count >= 3 ? GeneratorUtil.GetAttrArgEnumNum(a, 2, semantic) : ""));

            var sb = new StringBuilder();
            sb.Append(GeneratorSigniture).Append(
$@"using System;
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
").AppendForeach(nameValuePairs, nv =>
@$"            {nv.Name},
").Append(
$@"        }};

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string[] _allNames = new string[]
        {{
").AppendForeach(nameValuePairs, nv =>
@$"            ""{nv.Name}"",
").Append(
$@"        }};

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly (string Name, {structName} Value)[] _allNameValues = new (string Name, {structName} Value)[]
        {{
").AppendForeach(nameValuePairs, nv =>
@$"            (Name: ""{nv.Name}"", Value: {nv.Name}),
").Append(
$@"        }};

").AppendForeach(nameValuePairs, nv =>
$@"        /// <summary>{nv.Description}</summary>
        public static {structName} {nv.Name} => new {structName}(({underlyingType}){nv.Value});
").Append($@"

        private {structName}({underlyingType} value) => _value = value;

        public static ReadOnlySpan<{structName}> AllValuesSpan() => _allValues;

        public static IEnumerable<{structName}> AllValues() => _allValues;

        public static ReadOnlySpan<string> AllNamesSpan() => _allNames;

        public static IEnumerable<string> AllNames() => _allNames;

        public static ReadOnlySpan<(string Name, {structName} Value)> AllNameValuesSpan() => _allNameValues;

        public static IEnumerable<(string Name, {structName} Value)> AllNameValues() => _allNameValues;

        public override bool Equals(object obj) => obj is {structName} v && Equals(v);

        public bool Equals({structName} other) => _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==({structName} left, {structName} right) => left.Equals(right);

        public static bool operator !=({structName} left, {structName} right) => !(left == right);

        public override string ToString() => ToString(this);

        private static string ToString({structName} v)
        {{
            return ").AppendForeach(nameValuePairs, nv => @$"(v == {nv.Name}) ? ""{nv.Name}"" :
                   ").Append(@"v._value.ToString();
        }
    }
}
");
            context.AddSource(structName, SourceText.From(sb.ToString(), Encoding.UTF8));
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
