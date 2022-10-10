#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Elffy;

public interface IVertex
{
    static abstract bool TryGetVertexTypeData([MaybeNullWhen(false)] out VertexTypeData typeData);

    static abstract bool TryGetAccessor<TField>(VertexFieldSemantics semantics, out VertexFieldAccessor<TField> accessor) where TField : unmanaged;
    static abstract bool TryGetPositionAccessor(out VertexFieldAccessor<Vector3> accessor);
    static abstract bool TryGetUVAccessor(out VertexFieldAccessor<Vector2> accessor);
    static abstract bool TryGetNormalAccessor(out VertexFieldAccessor<Vector3> accessor);

    static abstract VertexFieldAccessor<TField> GetAccessor<TField>(VertexFieldSemantics semantics) where TField : unmanaged;
    static abstract VertexFieldAccessor<Vector3> GetPositionAccessor();
    static abstract VertexFieldAccessor<Vector2> GetUVAccessor();
    static abstract VertexFieldAccessor<Vector3> GetNormalAccessor();
}
