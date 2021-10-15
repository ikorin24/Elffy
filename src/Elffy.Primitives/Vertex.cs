#nullable enable
using System.Diagnostics;

namespace Elffy
{
    /// <summary>Vertex struct, which has position, normal, and uv.</summary>
    [DebuggerDisplay("{Position}")]
    [GenerateVertex]
    [VertexField("Position", typeof(Vector3), VertexSpecialField.Position, 0, VertexFieldMarshalType.Float, 3)]
    [VertexField("Normal", typeof(Vector3), VertexSpecialField.Normal, 12, VertexFieldMarshalType.Float, 3)]
    [VertexField("UV", typeof(Vector2), VertexSpecialField.UV, 24, VertexFieldMarshalType.Float, 2)]
    public partial struct Vertex
    {
    }

    /// <summary>Slim vertex struct, which has position and uv.</summary>
    /// <remarks>If you need "Normal", use <see cref="Vertex"/> instead.</remarks>
    [DebuggerDisplay("{Position}")]
    [GenerateVertex]
    [VertexField("Position", typeof(Vector3), VertexSpecialField.Position, 0, VertexFieldMarshalType.Float, 3)]
    [VertexField("UV", typeof(Vector2), VertexSpecialField.UV, 12, VertexFieldMarshalType.Float, 2)]
    public partial struct VertexSlim
    {
    }


    /// <summary>Skinned vertex struct, which has position, normal, uv, bone, weight, texture-index</summary>
    [DebuggerDisplay("{Position}")]
    [GenerateVertex]
    [VertexField("Position", typeof(Vector3), VertexSpecialField.Position, 0, VertexFieldMarshalType.Float, 3)]
    [VertexField("Normal", typeof(Vector3), VertexSpecialField.Normal, 12, VertexFieldMarshalType.Float, 3)]
    [VertexField("UV", typeof(Vector2), VertexSpecialField.UV, 24, VertexFieldMarshalType.Float, 2)]
    [VertexField("Bone", typeof(Vector4i), VertexSpecialField.Bone, 32, VertexFieldMarshalType.Int32, 4)]
    [VertexField("Weight", typeof(Vector4), VertexSpecialField.Weight, 48, VertexFieldMarshalType.Float, 4)]
    [VertexField("TextureIndex", typeof(int), VertexSpecialField.TextureIndex, 64, VertexFieldMarshalType.Int32, 1)]
    public partial struct SkinnedVertex
    {
    }
}
