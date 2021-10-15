#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;

namespace Elffy.Generator
{
    [Generator]
    public class CustomVertexGenerator : ISourceGenerator
    {
        private static readonly Regex _regexGVA = new Regex(@"^(global::)?(Elffy\.)?GenerateVertex(Attribute)?$");
        private static readonly Regex _regexVFA = new Regex(@"^(global::)?(Elffy\.)?VertexField(Attribute)?$");
        private static readonly string GeneratorSigniture = GeneratorUtil.GetGeneratorSigniture(typeof(CustomVertexGenerator));

        private const string SourceGVA =
@"using System;
using System.Diagnostics;

namespace Elffy
{
    [Conditional(""COMPILE_TIME_ONLY"")]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    internal sealed class GenerateVertexAttribute : Attribute
    {
        public GenerateVertexAttribute()
        {
        }
    }
}
";
        private const string SourceVFA =
@"using System;
using System.Diagnostics;

namespace Elffy
{
    [Conditional(""COMPILE_TIME_ONLY"")]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    internal sealed class VertexFieldAttribute : Attribute
    {
        public VertexFieldAttribute(string name, Type type, VertexSpecialField specialField, uint byteOffset, VertexFieldMarshalType marshalType, uint marshalCount)
        {
        }
    }
}
";
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new GenerateVertexSyntaxReciever());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("GenerateVertexAttribute", SourceText.From(GeneratorSigniture + SourceGVA, Encoding.UTF8));
            context.AddSource("VertexFieldAttribute", SourceText.From(GeneratorSigniture + SourceVFA, Encoding.UTF8));

            if(context.SyntaxReceiver is not GenerateVertexSyntaxReciever reciever) { return; }
            foreach(var attr in reciever.GenerateVertexAttrs) {
                try {
                    DumpGeneratedVertex(context, attr);
                }
                catch {
                }
            }
        }

        private static void DumpGeneratedVertex(GeneratorExecutionContext context, AttributeSyntax attr)
        {
            var semantic = context.Compilation.GetSemanticModel(attr.SyntaxTree);
            var (structNS, structName) = GeneratorUtil.GetAttrTargetStructName(attr, semantic);

            var fields = GeneratorUtil.GetAttrTargetStructSyntax(attr)
                .AttributeLists
                .SelectMany(l => l.Attributes)
                .Where(a => _regexVFA.IsMatch(a.Name.ToString()))
                .Select(a =>
                {
                    var name = GeneratorUtil.GetAttrArgString(a, 0, semantic);
                    var type = GeneratorUtil.GetAttrArgTypeFullName(a, 1, semantic);
                    var specialField = GeneratorUtil.GetAttrArgEnum<VertexSpecialField>(a, 2, semantic);
                    var offset = GeneratorUtil.GetAttrArgUInt(a, 3, semantic);
                    var marshalType = GeneratorUtil.GetAttrArgEnum<VertexFieldMarshalType>(a, 4, semantic);
                    var marshalCount = GeneratorUtil.GetAttrArgUInt(a, 5, semantic);
                    return new VertexFieldInfo(name, type, specialField, offset, marshalType, marshalCount);
                })
                .ToArray();

            var source = GeneratorSigniture + CreateSource(structNS, structName, fields);
            context.AddSource(structName, SourceText.From(source, Encoding.UTF8));
        }

        private static string CreateSource(string vertexNamespace, string vertexName, VertexFieldInfo[] fields)
        {
            var sb = new StringBuilder();
            sb.Append(GeneratorUtil.GetGeneratorSigniture(typeof(CustomVertexGenerator))).Append(
@$"using {nameof(Elffy)};
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace {vertexNamespace}
{{
    [StructLayout(LayoutKind.Explicit)]
    [{nameof(VertexAttribute)}]
    partial struct {vertexName} : IEquatable<{vertexName}>
    {{").AppendForeach(fields, f =>
@$"
        [FieldOffset({f.ByteOffset})]
        public {f.TypeFullName} {f.Name};
").Append(
@$"
        [ModuleInitializer]
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static void RegisterVertexTypeDataOnModuleInitialized()
        {{
            VertexMarshalHelper.Register<{vertexName}>(new[]
            {{
").AppendForeach(fields, f =>
$@"                new VertexFieldData(nameof({f.Name}), typeof({f.TypeFullName}), {nameof(VertexSpecialField)}.{f.SpecialField}, {f.ByteOffset}, {nameof(VertexFieldMarshalType)}.{f.Marshal}, {f.MarshalCount}),
").Append(
$@"            }}).ThrowIfError();
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public {vertexName}(").AppendForeach(fields, f => $"in {f.TypeFullName} {f.NameLowerCamelCase}", ", ").Append(@$")
        {{").AppendForeach(fields, f => @$"
            {f.Name} = {f.NameLowerCamelCase};").Append($@"
        }}

        public readonly override bool Equals(object? obj) => obj is {vertexName} vertex && Equals(vertex);

        public readonly bool Equals({vertexName} other) => ").AppendForeach(fields, f => $"{f.Name}.Equals(other.{f.Name})", " && ").Append(@$";

        public readonly override int GetHashCode() => HashCode.Combine(").AppendForeach(fields, f => f.Name, ", ").Append(@$");

        public static bool operator ==(in {vertexName} left, in {vertexName} right) => left.Equals(right);

        public static bool operator !=(in {vertexName} left, in {vertexName} right) => !(left == right);
    }}
}}
");
            return sb.ToString();
        }


        private sealed class GenerateVertexSyntaxReciever : ISyntaxReceiver
        {
            private readonly List<AttributeSyntax> _listGVA = new List<AttributeSyntax>();

            public IEnumerable<AttributeSyntax> GenerateVertexAttrs => _listGVA;

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if(syntaxNode.Kind() != SyntaxKind.Attribute) { return; }
                if(syntaxNode is not AttributeSyntax attr) { return; }
                var attrName = attr.Name.ToString();

                if(_regexGVA.IsMatch(attrName)) {
                    _listGVA.Add(attr);
                }
            }
        }


        private class VertexFieldInfo
        {
            public string Name { get; }
            public string TypeFullName { get; }
            public VertexSpecialField SpecialField { get; }
            public uint ByteOffset { get; }
            public VertexFieldMarshalType Marshal { get; }
            public uint MarshalCount { get; }

            public string NameLowerCamelCase
            {
                get
                {
                    if(string.IsNullOrEmpty(Name)) {
                        return Name;
                    }
                    var t = CultureInfo.CurrentCulture.TextInfo;
                    if(Name.All(c => char.IsUpper(c))) {
                        return Name.ToLower();
                    }
                    return t.ToLower(Name[0]) + Name.Substring(1);
                }
            }

            public VertexFieldInfo(string name, string typeFullName, VertexSpecialField specialField, uint byteOffset, VertexFieldMarshalType marshal, uint marshalCount)
            {
                Name = name;
                TypeFullName = typeFullName;
                SpecialField = specialField;
                ByteOffset = byteOffset;
                Marshal = marshal;
                MarshalCount = marshalCount;
            }
        }
    }
}
