#nullable enable
using System;
using System.Threading;
using U8Xml;

namespace Elffy.Markup;

public static class MarkupTranslator
{
    public static string Translate(XmlObject xml, string builderNS, string builderName, ITypeInfoStore typeInfoStore, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var context = new MarkupTranslatorContext(xml, builderNS, builderName, typeInfoStore, ct);
        throw new NotImplementedException();
    }

    //private static int GenerateInstanceFactory(XmlNode node, MarkupTranslatorContext context)
    //{
    //    context.CancellationToken.ThrowIfCancellationRequested();
    //    var controlType = context.GetTypeInfo("Elffy.UI.Control");
    //    var instanceType = context.NodeNameToTypeInfo(node);
    //    if(instanceType.IsSubclassOf(controlType)) {
    //        return GenerateControlInstanceFactory(node, instanceType, context);
    //    }
    //    else {
    //        return GenerateDefaultInstanceFactory(node, instanceType, context);
    //    }
    //}
}
