#nullable enable
using System.Diagnostics;

namespace Elffy
{
    /// <summary>Slim vertex struct, which has position and uv.</summary>
    /// <remarks>If you need "Normal", use <see cref="Vertex"/> instead.</remarks>
    [DebuggerDisplay("{Position}")]
    [GenerateCustomVertex(
        "Position", typeof(Vector3), VertexSpecialField.Position, 0, VertexFieldMarshalType.Float, 3,
        "UV", typeof(Vector2), VertexSpecialField.UV, 12, VertexFieldMarshalType.Float, 2
    )]
    public partial struct VertexSlim
    {
    }
}
