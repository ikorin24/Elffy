#nullable enable
using System;
using U8Xml;

namespace Elffy.Markup;

internal static class U8XmlMarkupExtensions
{
    public static TypeFullName GetTypeFullName(this XmlNode node)
    {
        var (nsName, name) = node.GetFullName();
        return new TypeFullName(nsName, name);
    }

    public static bool IsNamespaceAttr(this XmlAttribute attr)
    {
        ReadOnlySpan<byte> xmlns = new byte[] { (byte)'x', (byte)'m', (byte)'l', (byte)'n', (byte)'s' };
        ReadOnlySpan<byte> xmlns_colon = new byte[] { (byte)'x', (byte)'m', (byte)'l', (byte)'n', (byte)'s', (byte)':' };
        return attr.Name == xmlns || attr.Name.StartsWith(xmlns_colon);
    }
}
