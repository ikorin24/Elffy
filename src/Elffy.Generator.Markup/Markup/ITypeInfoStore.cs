#nullable enable

namespace Elffy.Markup;

public interface ITypeInfoStore
{
    bool TryGetTypeInfo(string typeName, out TypeInfo typeInfo, out TypeMemberInfoStore memberInfoStore);
}
