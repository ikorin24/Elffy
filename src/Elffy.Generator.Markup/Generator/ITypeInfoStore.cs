#nullable enable

namespace Elffy.Generator;

public interface ITypeInfoStore
{
    bool TryGetTypeInfo(string typeName, out TypeInfo typeInfo, out TypeMemberInfoStore memberInfoStore);
}
