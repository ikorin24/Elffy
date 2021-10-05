#nullable enable
using System.Diagnostics;

namespace Elffy
{
    [GenerateCustomVertex(
        "Position", typeof(Vector3),  VertexSpecialField.Position, 0,  VertexFieldMarshalType.Float, 3,
        "Normal",   typeof(Vector3),  VertexSpecialField.Normal,   12, VertexFieldMarshalType.Float, 3,
        "UV",       typeof(Vector2),  VertexSpecialField.UV,       24, VertexFieldMarshalType.Float, 2,
        "Bone",     typeof(Vector4i), VertexSpecialField.Bone,     32, VertexFieldMarshalType.Int32, 4,
        "Weight",   typeof(Vector4),  VertexSpecialField.Weight,   48, VertexFieldMarshalType.Float, 4
    )]
    [DebuggerDisplay("{Position}")]
    public partial struct RigVertex
    {
    }
}
