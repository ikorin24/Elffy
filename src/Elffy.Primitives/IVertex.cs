#nullable enable
namespace Elffy;

public interface IVertex
{
    /// <summary>Get vertex type data</summary>
    static abstract VertexTypeData VertexTypeData { get; }

    /// <summary>Get count of vertex semantic fields</summary>
    static abstract int FieldCount { get; }

    /// <summary>Get vertex size in bytes</summary>
    static abstract int VertexSize { get; }

    /// <summary>Get whether the vertex has the specified semantics field.</summary>
    /// <param name="semantics">field semantics</param>
    /// <returns>whether the vertex has the specified semantics field</returns>
    static abstract bool HasField(VertexFieldSemantics semantics);

    /// <summary>Get the accessor of the specified semantics</summary>
    /// <typeparam name="TField">field type</typeparam>
    /// <param name="semantics">field semantics</param>
    /// <param name="accessor">accessor</param>
    /// <returns>true if success in getting the accessor</returns>
    static abstract bool TryGetAccessor<TField>(VertexFieldSemantics semantics, out VertexFieldAccessor<TField> accessor) where TField : unmanaged;

    /// <summary>Get the accessor of position</summary>
    /// <param name="accessor">accessor</param>
    /// <returns>true if success in getting the accessor</returns>
    static abstract bool TryGetPositionAccessor(out VertexFieldAccessor<Vector3> accessor);

    /// <summary>Get the accessor of UV</summary>
    /// <param name="accessor">accessor</param>
    /// <returns>true if success in getting the accessor</returns>
    static abstract bool TryGetUVAccessor(out VertexFieldAccessor<Vector2> accessor);

    /// <summary>Get the accessor of normal</summary>
    /// <param name="accessor">accessor</param>
    /// <returns>true if success in getting the accessor</returns>
    static abstract bool TryGetNormalAccessor(out VertexFieldAccessor<Vector3> accessor);

    /// <summary>Get the accessor of the specified semantics</summary>
    /// <typeparam name="TField">field type</typeparam>
    /// <param name="semantics">field semantics</param>
    /// <returns>accessor</returns>
    static abstract VertexFieldAccessor<TField> GetAccessor<TField>(VertexFieldSemantics semantics) where TField : unmanaged;

    /// <summary>Get the accessor of position</summary>
    /// <returns>accessor</returns>
    static abstract VertexFieldAccessor<Vector3> GetPositionAccessor();

    /// <summary>Get the accessor of uv</summary>
    /// <returns>accessor</returns>
    static abstract VertexFieldAccessor<Vector2> GetUVAccessor();

    /// <summary>Get the accessor of normal</summary>
    /// <returns>accessor</returns>
    static abstract VertexFieldAccessor<Vector3> GetNormalAccessor();
}
