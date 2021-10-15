#nullable enable
using System.Diagnostics;

namespace Elffy
{
    [GenerateVertex]
    [VertexField("Position", typeof(Vector3), VertexSpecialField.Position, 0, VertexFieldMarshalType.Float, 3)]
    [VertexField("Normal", typeof(Vector3), VertexSpecialField.Normal, 12, VertexFieldMarshalType.Float, 3)]
    [VertexField("UV", typeof(Vector2), VertexSpecialField.UV, 24, VertexFieldMarshalType.Float, 2)]
    [VertexField("Bone", typeof(Vector4i), VertexSpecialField.Bone, 32, VertexFieldMarshalType.Int32, 4)]
    [VertexField("Weight", typeof(Vector4), VertexSpecialField.Weight, 48, VertexFieldMarshalType.Float, 4)]
    [DebuggerDisplay("{Position}")]
    public partial struct RigVertex
    {
    }
}
