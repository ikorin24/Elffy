#nullable enable
using System;
using System.Threading;
using U8Xml;

namespace Elffy.Generator;

public static class MarkupTranslator
{
    public static string TranslateToCode(XmlObject xml, string nameSpace, string className, ITypeInfoStore typeInfoStore, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        throw new NotImplementedException();
    }
}
