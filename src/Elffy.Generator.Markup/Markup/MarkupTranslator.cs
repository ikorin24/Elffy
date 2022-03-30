#nullable enable
using Elffy.Generator;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using U8Xml;

namespace Elffy.Markup;

public static class MarkupTranslator
{
    private const int SkippedMethodID = -1;

    //[global::System.Obsolete("", true)]
    public static void Translate(
        XmlObject xml,
        ITypeInfoStore typeInfoStore,
        IMarkupTranslationResultHolder resultHolder,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();


        if(TryGetBuilderName(xml, out var builderNS, out var builderName) == false) {
            resultHolder.AddDiagnostic(DiagnosticHelper.BuilderNotSpecified());
            return;
        }
        var sourceBuilder = new SourceStringBuilder(builderNS, builderName);
        var context = new MarkupTranslatorContext(xml, sourceBuilder, typeInfoStore, resultHolder, ct);
        sourceBuilder.Header.AppendLine("#nullable enable");

        var rootNode = xml.Root;
        var rootMethod = sourceBuilder.CreateMethodBuilder(2, out _);
        var (id, returnedType) = GenerateFactoryMethodCode(rootNode, TypeInfo.Null, context);

        if(id == SkippedMethodID || returnedType.IsNull) {
            rootMethod.AppendLine("[global::System.Obsolete(\"The markup file was not translated to code successfully.\", true)]");
            if(returnedType.IsNull) {
                rootMethod.AppendLine($"public static async global::Cysharp.Threading.Tasks.UniTask Create()");
            }
            else {
                rootMethod.AppendLine($"public static async global::Cysharp.Threading.Tasks.UniTask<global::{returnedType}> Create()");
            }
            rootMethod.AppendLine("{ throw new global::System.InvalidOperationException(\"The markup file was not translated to code successfully.\"); }");
            return;
        }

        rootMethod.AppendLine($"public static async global::Cysharp.Threading.Tasks.UniTask<global::{returnedType}> Create()");
        rootMethod.AppendLine("{");
        rootMethod.IncrementIndent();
        rootMethod.AppendLine("var context = new Context();");
        rootMethod.AppendLine("try {");
        rootMethod.AppendLine($"    var obj = __F{id}(ref context, parent);");
        rootMethod.AppendLine("    await context.WhenAllTask();");
        rootMethod.AppendLine("} finally { context.Dispose(); }");
        rootMethod.AppendLine("return obj;");
        rootMethod.DecrementIndent();
        rootMethod.AppendLine("}");

        var result = SourceText.From(sourceBuilder.ToString(), Encoding.UTF8);
        resultHolder.SetResult(result);

    }

    private static bool TryGetBuilderName(XmlObject xml, [MaybeNullWhen(false)] out string builderNS, [MaybeNullWhen(false)] out string builderName)
    {
        // x:builder="Foo.Bar.Baz" xmlns:x="Elffy.Markup.Extensions"

        if(xml.Root.TryFindAttribute("Elffy.Markup.Extensions", "builder", out var attr) == false) {
            builderNS = null;
            builderName = null;
            return false;
        }
        var value = attr.Value;
        int i;
        for(i = value.Length - 1; i >= 0; i--) {
            if(value[i] == (byte)'.') {
                break;
            }
        }
        builderNS = value.Slice(0, i).ToString();
        builderName = value.Slice(i + 1).ToString();
        return true;
    }

    private static (int MethodID, TypeInfo ReturnedType) GenerateFactoryMethodCode(XmlNode node, TypeInfo callerObjType, MarkupTranslatorContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var typeInfoStore = context.TypeInfoStore;
        if(typeInfoStore.TryGetTypeInfo(node.GetTypeFullName().ToString(), out var instanceType, out var members) == false) {
            context.AddDiagnostic(DiagnosticHelper.TypeNotFound(node.GetTypeFullName().ToString()));
            return (SkippedMethodID, TypeInfo.Null);
        }

        var mb = context.SourceBuilder.CreateMethodBuilder(2, out var methodId);

        if(callerObjType.IsNull) {
            mb.AppendLine($"private static global::{instanceType.Name} __F{methodId}(ref Context context)");
        }
        else {
            mb.AppendLine($"private static global::{instanceType.Name} __F{methodId}(ref Context context, in global::{callerObjType.Name} caller)");
        }
        mb.AppendLine("{");
        mb.IncrementIndent();
        mb.AppendLine($"var obj = new global::{instanceType.Name}();");

        foreach(var attr in node.Attributes) {
            if(attr.IsNamespaceAttr()) {
                continue;
            }
            var propName = attr.Name.ToString();
            var literal = context.XmlEntities.ResolveToString(attr.Value);
            if(members.TryGetMember(propName, out var propType) == false) {
                context.AddDiagnostic(DiagnosticHelper.SettableMemberNotFound(instanceType.Name, propName));
                // Skip this property
                continue;
            }
            if(propType.TryGetLiteralCode(literal, out var literalCode, out var diagnostic) == false) {
                context.AddDiagnostic(diagnostic);
                // Skip this property
                continue;
            }
            mb.AppendLine($"obj.{propName} = {literalCode};");
        }


        foreach(var childNode in node.Children) {
            if(typeInfoStore.IsPropertyNode(childNode, out var propOwnerType, out var propName)) {
                if(members.TryGetMember(propName.ToString(), out var propType) == false) {
                    context.AddDiagnostic(DiagnosticHelper.SettableMemberNotFound(instanceType.Name, propName.ToString()));

                    // Skip this property and child values of it.
                    continue;
                }
                var instanceCode = (propOwnerType.Name == instanceType.Name) ? "obj" : $"((global::{propOwnerType.Name})obj)";

                if(childNode.Children.Count == 1) {
                    var (id, _) = GenerateFactoryMethodCode(childNode.FirstChild.Value, instanceType, context);
                    if(id == SkippedMethodID) {
                        // Skip this property and child values of it.
                        continue;
                    }
                    else {
                        mb.AppendLine($"{instanceCode}.{propName} = __F{id}(ref context, obj);");
                    }
                }
                else if(childNode.Children.Count == 0) {
                    var literal = context.XmlEntities.ResolveToString(childNode.InnerText);
                    if(propType.TryGetLiteralCode(literal, out var code, out var diagnostic) == false) {
                        context.AddDiagnostic(diagnostic);
                        // Skip this property
                        continue;
                    }
                    mb.AppendLine($"{instanceCode}.{propName} = {code};");
                }
                else {
                    context.AddDiagnostic(DiagnosticHelper.MutipleValuesNotSupported(instanceType.Name, propName.ToString()));
                    continue;
                }
            }
            else {
                var (id, _) = GenerateFactoryMethodCode(childNode, instanceType, context);
                var contentCode = id != SkippedMethodID ? $"__F{id}(ref context, obj)" : "default!";
                if(instanceType.TryGetContentSetterCode(contentCode, out var result)) {
                    mb.AppendLine(result);
                }
                else {
                    context.AddDiagnostic(DiagnosticHelper.DirectContentNotSupported(instanceType.Name));
                    continue;
                }
            }
        }
        mb.AppendLine("return obj;");
        mb.DecrementIndent();
        mb.AppendLine(@"}
");
        return (methodId, instanceType);
    }
}
