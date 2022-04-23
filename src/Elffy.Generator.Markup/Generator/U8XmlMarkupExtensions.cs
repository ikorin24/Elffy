#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using U8Xml;

namespace Elffy.Generator;

internal static class U8XmlMarkupExtensions
{
    public static TypeFullName GetTypeFullName(this XmlNode node)
    {
        var (nsName, name) = node.GetFullName();
        return new TypeFullName(nsName, name);
    }

    public static bool TryGetTypeFullNameAndMember(this XmlAttribute attr, out TypeFullName typeFullName, out RawString memberName)
    {
        if(attr.TryGetFullName(out var ns, out var typeAndMember) == false) {
            goto FAILURE;
        }
        var i = typeAndMember.LastIndexOf((byte)'.');
        if(i < 0) {
            goto FAILURE;
        }
        var typeName = typeAndMember.Slice(0, i);
        memberName = typeAndMember.Slice(i + 1);
        if(typeName.IsEmpty || memberName.IsEmpty) {
            goto FAILURE;
        }
        typeFullName = new TypeFullName(ns, typeName);
        return true;
    FAILURE:
        typeFullName = default;
        memberName = default;
        return false;
    }

    public static bool IsNamespaceAttr(this XmlAttribute attr)
    {
        ReadOnlySpan<byte> xmlns = new byte[] { (byte)'x', (byte)'m', (byte)'l', (byte)'n', (byte)'s' };
        ReadOnlySpan<byte> xmlns_colon = new byte[] { (byte)'x', (byte)'m', (byte)'l', (byte)'n', (byte)'s', (byte)':' };
        return attr.Name == xmlns || attr.Name.StartsWith(xmlns_colon);
    }

    public static bool Contains(this RawString rawString, byte c)
    {
        foreach(var x in rawString.AsSpan()) {
            if(x == c) { return true; }
        }
        return false;
    }

    public static bool IsAttachedMemberAttr(this XmlAttribute attr, TypeDataStore store, out RawString memberName, [MaybeNullWhen(false)] out TypeData ownerType)
    {
        if(attr.TryGetTypeFullNameAndMember(out var typeFullName, out memberName)) {
            if(store.TryGetTypeData(typeFullName.ToString(), out ownerType)) {
                return true;
            }
        }
        ownerType = null;
        return false;
    }

    public static int LastIndexOf(this RawString str, byte c)
    {
        for(int i = str.Length - 1; i >= 0; i--) {
            if(str[i] == c) {
                return i;
            }
        }
        return -1;
    }
}
