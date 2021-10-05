#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Elffy.Generator
{
    [Generator]
    public class CustomVertexGenerator : ISourceGenerator
    {
        private readonly Regex _attrRegex = new Regex(@"^(global::)?(Elffy\.)?GenerateCustomVertex(Attribute)?$");

        private const string AttributeSource =
@"using System;
using System.Diagnostics;

namespace Elffy
{
    [Conditional(""COMPILE_TIME_ONLY"")]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    internal sealed class GenerateCustomVertexAttribute : Attribute
    {
        public GenerateCustomVertexAttribute(string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1)
        { }

        public GenerateCustomVertexAttribute(string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
                                             string name2, Type type2, VertexSpecialField specialField2, uint byteOffset2, VertexFieldMarshalType marshal2, uint marshalCount2)
        { }

        public GenerateCustomVertexAttribute(string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
                                             string name2, Type type2, VertexSpecialField specialField2, uint byteOffset2, VertexFieldMarshalType marshal2, uint marshalCount2,
                                             string name3, Type type3, VertexSpecialField specialField3, uint byteOffset3, VertexFieldMarshalType marshal3, uint marshalCount3)
        { }

        public GenerateCustomVertexAttribute(string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
                                             string name2, Type type2, VertexSpecialField specialField2, uint byteOffset2, VertexFieldMarshalType marshal2, uint marshalCount2,
                                             string name3, Type type3, VertexSpecialField specialField3, uint byteOffset3, VertexFieldMarshalType marshal3, uint marshalCount3,
                                             string name4, Type type4, VertexSpecialField specialField4, uint byteOffset4, VertexFieldMarshalType marshal4, uint marshalCount4)
        { }

        public GenerateCustomVertexAttribute(string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
                                             string name2, Type type2, VertexSpecialField specialField2, uint byteOffset2, VertexFieldMarshalType marshal2, uint marshalCount2,
                                             string name3, Type type3, VertexSpecialField specialField3, uint byteOffset3, VertexFieldMarshalType marshal3, uint marshalCount3,
                                             string name4, Type type4, VertexSpecialField specialField4, uint byteOffset4, VertexFieldMarshalType marshal4, uint marshalCount4,
                                             string name5, Type type5, VertexSpecialField specialField5, uint byteOffset5, VertexFieldMarshalType marshal5, uint marshalCount5)
        { }

        public GenerateCustomVertexAttribute(string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
                                             string name2, Type type2, VertexSpecialField specialField2, uint byteOffset2, VertexFieldMarshalType marshal2, uint marshalCount2,
                                             string name3, Type type3, VertexSpecialField specialField3, uint byteOffset3, VertexFieldMarshalType marshal3, uint marshalCount3,
                                             string name4, Type type4, VertexSpecialField specialField4, uint byteOffset4, VertexFieldMarshalType marshal4, uint marshalCount4,
                                             string name5, Type type5, VertexSpecialField specialField5, uint byteOffset5, VertexFieldMarshalType marshal5, uint marshalCount5,
                                             string name6, Type type6, VertexSpecialField specialField6, uint byteOffset6, VertexFieldMarshalType marshal6, uint marshalCount6)
        { }

        public GenerateCustomVertexAttribute(string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
                                             string name2, Type type2, VertexSpecialField specialField2, uint byteOffset2, VertexFieldMarshalType marshal2, uint marshalCount2,
                                             string name3, Type type3, VertexSpecialField specialField3, uint byteOffset3, VertexFieldMarshalType marshal3, uint marshalCount3,
                                             string name4, Type type4, VertexSpecialField specialField4, uint byteOffset4, VertexFieldMarshalType marshal4, uint marshalCount4,
                                             string name5, Type type5, VertexSpecialField specialField5, uint byteOffset5, VertexFieldMarshalType marshal5, uint marshalCount5,
                                             string name6, Type type6, VertexSpecialField specialField6, uint byteOffset6, VertexFieldMarshalType marshal6, uint marshalCount6,
                                             string name7, Type type7, VertexSpecialField specialField7, uint byteOffset7, VertexFieldMarshalType marshal7, uint marshalCount7)
        { }

        public GenerateCustomVertexAttribute(string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
                                             string name2, Type type2, VertexSpecialField specialField2, uint byteOffset2, VertexFieldMarshalType marshal2, uint marshalCount2,
                                             string name3, Type type3, VertexSpecialField specialField3, uint byteOffset3, VertexFieldMarshalType marshal3, uint marshalCount3,
                                             string name4, Type type4, VertexSpecialField specialField4, uint byteOffset4, VertexFieldMarshalType marshal4, uint marshalCount4,
                                             string name5, Type type5, VertexSpecialField specialField5, uint byteOffset5, VertexFieldMarshalType marshal5, uint marshalCount5,
                                             string name6, Type type6, VertexSpecialField specialField6, uint byteOffset6, VertexFieldMarshalType marshal6, uint marshalCount6,
                                             string name7, Type type7, VertexSpecialField specialField7, uint byteOffset7, VertexFieldMarshalType marshal7, uint marshalCount7,
                                             string name8, Type type8, VertexSpecialField specialField8, uint byteOffset8, VertexFieldMarshalType marshal8, uint marshalCount8)
        { }
    }
}
";

        public void Execute(GeneratorExecutionContext context)
        {
            // Dump the attribute.
            var attributeSource = GeneratorUtil.GetGeneratorSigniture(typeof(CustomVertexGenerator)) + AttributeSource;
            context.AddSource("GenerateCustomVertexAttribute", SourceText.From(attributeSource, Encoding.UTF8));

            // Ignore exceptions anytime because source generator must dump the attribute completely.
            // (Invalid code is often input when incremental build of IDE is enabled, which occurs many exceptions.)
            try {
                var compilation = context.Compilation;
                var attrs = compilation
                        .SyntaxTrees
                        .SelectMany(s => s.GetRoot().DescendantNodes())
                        .Where(s => s.IsKind(SyntaxKind.Attribute))
                        .OfType<AttributeSyntax>()
                        .Where(s => _attrRegex.IsMatch(s.Name.ToString()))
                        .ToArray();
                if(!attrs.Any()) {
                    return;
                }
                foreach(var attr in attrs) {
                    if(attr.ArgumentList is null) { throw new Exception("Can't be null here."); }
                    var semanticModel = compilation.GetSemanticModel(attr.SyntaxTree);
                    var fields = ParseArgs(attr, semanticModel, out var vertexNamespace, out var vertexName);
                    var source = CreateSource(vertexNamespace, vertexName, fields);
                    context.AddSource(vertexName, SourceText.From(source, Encoding.UTF8));
                }
            }
            catch {
                // Ignore exceptions.
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // nop
        }

        private static (string structNamespace, string structName) GetAttrTargetStructName(AttributeSyntax attr, SemanticModel attrSemantic)
        {
            var parent = attr.Parent;
            while(true) {
                if(parent == null) {
                    throw new Exception();
                }
                if(parent.Kind() == SyntaxKind.StructDeclaration) {
                    var structDeclaration = attrSemantic.GetDeclaredSymbol(parent) ?? throw new Exception();
                    var structNamespace = structDeclaration.ContainingNamespace.ToString();
                    var structName = structDeclaration.Name;
                    return (structNamespace, structName);
                }
                parent = parent.Parent;
            }
        }

        private static VertexFieldInfo[] ParseArgs(AttributeSyntax attr, SemanticModel attrSemantic, out string vertexNamespace, out string vertexName)
        {
            if(attr.ArgumentList is null) { throw new Exception("Can not be null argument list."); }
            const int N = 6;
            (vertexNamespace, vertexName) = GetAttrTargetStructName(attr, attrSemantic);
            var fieldCount = attr.ArgumentList.Arguments.Count / N;
            var fields = new VertexFieldInfo[fieldCount];
            for(int i = 0; i < fields.Length; i++) {
                var name = GeneratorUtil.GetAttrArgString(attr, i * N, attrSemantic);
                var typeFullName = GeneratorUtil.GetAttrArgTypeFullName(attr, i * N + 1, attrSemantic);
                var specialField = GeneratorUtil.GetAttrArgEnum<VertexSpecialField>(attr, i * N + 2, attrSemantic);
                var byteOffset = GeneratorUtil.GetAttrArgUInt(attr, i * N + 3, attrSemantic);
                var marshal = GeneratorUtil.GetAttrArgEnum<VertexFieldMarshalType>(attr, i * N + 4, attrSemantic);
                var marshalCount = GeneratorUtil.GetAttrArgUInt(attr, i * N + 5, attrSemantic);
                if(marshalCount <= 0 || marshalCount > 4) {
                    throw new Exception("Marshal count must be 1, 2, 3 or 4.");
                }
                fields[i] = new VertexFieldInfo(name, typeFullName, specialField, byteOffset, marshal, marshalCount);
            }
            return fields;
        }

        private string CreateSource(string vertexNamespace, string vertexName, VertexFieldInfo[] fields)
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
    [{nameof(VertexLikeAttribute)}]
    [StructLayout(LayoutKind.Explicit)]
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
    }

    internal class VertexFieldInfo
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
