#nullable enable
using System.Diagnostics.CodeAnalysis;
using U8Xml;
using Elffy.Generator;

namespace Elffy.Markup;

public interface ITypeInfoStore
{
    bool TryGetTypeInfo(string typeName, out TypeInfo typeInfo, out TypeMemberDic memberInfoStore);
}

public static class TypeInfoStoreExtensions
{
    public static bool IsPropertyNode(this RoslynTypeInfoStore store, XmlNode node, out TypeInfo propOwnerType, out RawString propName)
    {
        var (nsName, tmp) = node.GetFullName();
        (var name, propName) = tmp.Split2((byte)'.');
        if(propName.IsEmpty) {
            propOwnerType = default;
            return false;
        }
        if(store.TryGetTypeInfo(new TypeFullName(nsName, name).ToString(), out var members) == false) {
            propOwnerType = default;
            return false;
        }
        propOwnerType = members.OwnerType;
        return true;
    }
}
