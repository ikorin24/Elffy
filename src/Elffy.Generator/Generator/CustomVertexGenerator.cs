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
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    internal sealed class GenerateCustomVertexAttribute : Attribute
    {
        public GenerateCustomVertexAttribute(string typeFullName,
                                             string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1)
        { }

        public GenerateCustomVertexAttribute(string typeFullName,
                                             string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
                                             string name2, Type type2, VertexSpecialField specialField2, uint byteOffset2, VertexFieldMarshalType marshal2, uint marshalCount2)
        { }

        public GenerateCustomVertexAttribute(string typeFullName,
                                             string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
                                             string name2, Type type2, VertexSpecialField specialField2, uint byteOffset2, VertexFieldMarshalType marshal2, uint marshalCount2,
                                             string name3, Type type3, VertexSpecialField specialField3, uint byteOffset3, VertexFieldMarshalType marshal3, uint marshalCount3)
        { }

        public GenerateCustomVertexAttribute(string typeFullName,
                                             string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
                                             string name2, Type type2, VertexSpecialField specialField2, uint byteOffset2, VertexFieldMarshalType marshal2, uint marshalCount2,
                                             string name3, Type type3, VertexSpecialField specialField3, uint byteOffset3, VertexFieldMarshalType marshal3, uint marshalCount3,
                                             string name4, Type type4, VertexSpecialField specialField4, uint byteOffset4, VertexFieldMarshalType marshal4, uint marshalCount4)
        { }

        public GenerateCustomVertexAttribute(string typeFullName,
                                             string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
                                             string name2, Type type2, VertexSpecialField specialField2, uint byteOffset2, VertexFieldMarshalType marshal2, uint marshalCount2,
                                             string name3, Type type3, VertexSpecialField specialField3, uint byteOffset3, VertexFieldMarshalType marshal3, uint marshalCount3,
                                             string name4, Type type4, VertexSpecialField specialField4, uint byteOffset4, VertexFieldMarshalType marshal4, uint marshalCount4,
                                             string name5, Type type5, VertexSpecialField specialField5, uint byteOffset5, VertexFieldMarshalType marshal5, uint marshalCount5)
        { }

        public GenerateCustomVertexAttribute(string typeFullName,
                                             string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
                                             string name2, Type type2, VertexSpecialField specialField2, uint byteOffset2, VertexFieldMarshalType marshal2, uint marshalCount2,
                                             string name3, Type type3, VertexSpecialField specialField3, uint byteOffset3, VertexFieldMarshalType marshal3, uint marshalCount3,
                                             string name4, Type type4, VertexSpecialField specialField4, uint byteOffset4, VertexFieldMarshalType marshal4, uint marshalCount4,
                                             string name5, Type type5, VertexSpecialField specialField5, uint byteOffset5, VertexFieldMarshalType marshal5, uint marshalCount5,
                                             string name6, Type type6, VertexSpecialField specialField6, uint byteOffset6, VertexFieldMarshalType marshal6, uint marshalCount6)
        { }

        public GenerateCustomVertexAttribute(string typeFullName,
                                             string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
                                             string name2, Type type2, VertexSpecialField specialField2, uint byteOffset2, VertexFieldMarshalType marshal2, uint marshalCount2,
                                             string name3, Type type3, VertexSpecialField specialField3, uint byteOffset3, VertexFieldMarshalType marshal3, uint marshalCount3,
                                             string name4, Type type4, VertexSpecialField specialField4, uint byteOffset4, VertexFieldMarshalType marshal4, uint marshalCount4,
                                             string name5, Type type5, VertexSpecialField specialField5, uint byteOffset5, VertexFieldMarshalType marshal5, uint marshalCount5,
                                             string name6, Type type6, VertexSpecialField specialField6, uint byteOffset6, VertexFieldMarshalType marshal6, uint marshalCount6,
                                             string name7, Type type7, VertexSpecialField specialField7, uint byteOffset7, VertexFieldMarshalType marshal7, uint marshalCount7)
        { }

        public GenerateCustomVertexAttribute(string typeFullName,
                                             string name1, Type type1, VertexSpecialField specialField1, uint byteOffset1, VertexFieldMarshalType marshal1, uint marshalCount1,
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
                    var fields = ParseArgs(attr,
                                           compilation.GetSemanticModel(attr.SyntaxTree),
                                           out var vertexTypeName);
                    var source = CreateSource(vertexTypeName, fields);
                    context.AddSource(vertexTypeName, SourceText.From(source, Encoding.UTF8));
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

        private static VertexFieldInfo[] ParseArgs(AttributeSyntax attr, SemanticModel attrSemantic, out string vertexTypeName)
        {
            if(attr.ArgumentList is null) {
                throw new Exception("Can not be null argument list.");
            }
            var args = attr.ArgumentList.Arguments;
            vertexTypeName = attrSemantic.GetConstantValue(args[0].Expression).ToString();
            const int N = 6;

            var fieldCount = (args.Count - 1) / N;
            var fields = new VertexFieldInfo[fieldCount];
            for(int i = 0; i < fields.Length; i++) {
                var name = attrSemantic.GetConstantValue(args[i * N + 1].Expression).Value?.ToString() ?? throw new Exception();
                var typeFullName = (args[i * N + 2].Expression is TypeOfExpressionSyntax syntax) ?
                                   attrSemantic.GetSymbolInfo(syntax.Type).Symbol?.ToString() ?? throw new Exception()
                                   : throw new Exception();
                var specialField = (VertexSpecialField)attrSemantic.GetConstantValue(args[i * N + 3].Expression).Value!;
                if(!uint.TryParse(attrSemantic.GetConstantValue(args[i * N + 4].Expression).Value!.ToString(), out var offset)) {
                    throw new Exception();
                }
                
                var marshal = (VertexFieldMarshalType)attrSemantic.GetConstantValue(args[i * N + 5].Expression).Value!;

                if(!uint.TryParse(attrSemantic.GetConstantValue(args[i * N + 6].Expression).Value!.ToString(), out var marshalCount)) {
                    throw new Exception();
                }

                if(marshalCount <= 0 || marshalCount > 4) {
                    throw new Exception("Marshal count must be 1, 2, 3 or 4.");
                }
                fields[i] = new(name, specialField, typeFullName, offset, marshal, marshalCount);
            }
            return fields;
        }

        private string CreateSource(string vertexTypeName, VertexFieldInfo[] fields)
        {
            var ns = vertexTypeName.Substring(0, vertexTypeName.LastIndexOf('.'));
            var typeName = vertexTypeName.Substring(ns.Length + 1);

            var sb = new StringBuilder();
            sb.Append(GeneratorUtil.GetGeneratorSigniture(typeof(CustomVertexGenerator)));
            sb.Append(
@$"using {nameof(Elffy)};
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace {ns}
{{
");
            sb.Append(
@$"    [{nameof(VertexLikeAttribute)}]
    [StructLayout(LayoutKind.Explicit)]
    public partial struct {typeName} : IEquatable<{typeName}>
    {{
");
            foreach(var field in fields) {
                sb.Append(
@$"        [FieldOffset({field.ByteOffset})]
        public {field.TypeFullName} {field.Name};
");
            }

            sb.Append(
@$"
        [ModuleInitializer]
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static void RegisterVertexTypeDataOnModuleInitialized()
        {{
            VertexMarshalHelper.Register<{typeName}>(
                fieldName => fieldName switch
            {{
");

            foreach(var field in fields) {
                sb.Append(
@$"                nameof({field.Name}) => ({field.ByteOffset}, {nameof(VertexFieldMarshalType)}.{field.Marshal}, {field.MarshalCount}),
");
            }

            sb.Append(
@"                _ => throw new ArgumentException(),
            },
                specialField => specialField switch
            {
");
            var specials = fields.Where(f => f.SpecialField != VertexSpecialField.NotSpecified)
                                 .OrderBy(f => f.SpecialField)
                                 .ToDictionary(f => f.SpecialField, f => f.Name);
            foreach(var keyValue in specials) {
                var s = keyValue.Key;
                var n = keyValue.Value;
                sb.Append(
@$"                {nameof(VertexSpecialField)}.{s} => nameof({n}),
");
            }

            sb.Append(
@$"                _ => """",
            }}).ThrowIfError();
        }}
");

            sb.Append(
@$"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public {typeName}({string.Join(", ", fields.Select(f => $"in {f.TypeFullName} {f.NameLowerCamelCase}"))})
        {{
");
            foreach(var field in fields) {
                sb.Append(
@$"            {field.Name} = {field.NameLowerCamelCase};
");
            }
            sb.Append(
@"        }
");
            
            sb.Append(
@$"
        public readonly override bool Equals(object? obj) => obj is {typeName} vertex && Equals(vertex);

        public readonly bool Equals({typeName} other) => {string.Join(" && ", fields.Select(f => $"{f.Name}.Equals(other.{f.Name})"))};

        public readonly override int GetHashCode() => HashCode.Combine({string.Join(", ", fields.Select(f => f.Name))});

        public static bool operator ==(in {typeName} left, in {typeName} right) => left.Equals(right);

        public static bool operator !=(in {typeName} left, in {typeName} right) => !(left == right);
    }}
}}
");
            return sb.ToString();
        }
    }

    internal class VertexFieldInfo
    {
        public string Name { get; }
        public VertexSpecialField SpecialField { get; }
        public string TypeFullName { get; }
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

        public VertexFieldInfo(string name, VertexSpecialField specialField, string typeFullName, uint byteOffset, VertexFieldMarshalType marshal, uint marshalCount)
        {
            Name = name;
            SpecialField = specialField;
            TypeFullName = typeFullName;
            ByteOffset = byteOffset;
            Marshal = marshal;
            MarshalCount = marshalCount;
        }
    }
}
