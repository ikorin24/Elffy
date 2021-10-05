#nullable enable
using System.Diagnostics;

namespace Elffy
{
    /// <summary>Vertex struct, which has position, normal, and uv.</summary>
    [DebuggerDisplay("{Position}")]
    [GenerateCustomVertex(
        "Position", typeof(Vector3), VertexSpecialField.Position, 0, VertexFieldMarshalType.Float, 3,
        "Normal", typeof(Vector3), VertexSpecialField.Normal, 12, VertexFieldMarshalType.Float, 3,
        "UV", typeof(Vector2), VertexSpecialField.UV, 24, VertexFieldMarshalType.Float, 2
    )]
    public partial struct Vertex
    {
    }
}
