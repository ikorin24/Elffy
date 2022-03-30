#nullable enable
using System.Diagnostics.CodeAnalysis;
using U8Xml;

namespace Elffy.Markup;

public interface ITypeInfoStore
{
    bool TryGetTypeInfo(string typeName, out TypeInfo typeInfo, out TypeMemberDic memberInfoStore);
}

public static class TypeInfoStoreExtensions
{
    public static bool IsPropertyNode(this ITypeInfoStore store, XmlNode node, [MaybeNullWhen(false)] out TypeInfo propOwnerType, out RawString propName)
    {
        var (nsName, tmp) = node.GetFullName();
        (var name, propName) = tmp.Split2((byte)'.');
        if(propName.IsEmpty) {
            propOwnerType = default;
            return false;
        }
        return store.TryGetTypeInfo(new TypeFullName(nsName, name).ToString(), out propOwnerType, out _);
    }
}
