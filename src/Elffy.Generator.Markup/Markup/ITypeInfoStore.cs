#nullable enable

using Elffy.Generator;

namespace Elffy.Markup;

public interface ITypeInfoStore
{
    bool TryGetTypeInfo(string typeName, out TypeInfo typeInfo, out TypeMemberDic memberInfoStore);
}
