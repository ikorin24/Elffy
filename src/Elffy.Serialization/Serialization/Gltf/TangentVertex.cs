#nullable enable
using System.Diagnostics;

namespace Elffy.Serialization.Gltf;

[DebuggerDisplay("{Position}")]
[GenerateVertex]
[VertexField("Position", typeof(Vector3), VertexFieldSemantics.Position, 0, VertexFieldMarshalType.Float, 3)]
[VertexField("Normal", typeof(Vector3), VertexFieldSemantics.Normal, 12, VertexFieldMarshalType.Float, 3)]
[VertexField("UV", typeof(Vector2), VertexFieldSemantics.UV, 24, VertexFieldMarshalType.Float, 2)]
[VertexField("Tangent", typeof(Vector3), VertexFieldSemantics.Tangent, 32, VertexFieldMarshalType.Float, 3)]
internal partial struct TangentVertex
{
}

[DebuggerDisplay("{Position}")]
[GenerateVertex]
[VertexField("Position", typeof(Vector3), VertexFieldSemantics.Position, 0, VertexFieldMarshalType.Float, 3)]
[VertexField("Normal", typeof(Vector3), VertexFieldSemantics.Normal, 12, VertexFieldMarshalType.Float, 3)]
[VertexField("UV", typeof(Vector2), VertexFieldSemantics.UV, 24, VertexFieldMarshalType.Float, 2)]
[VertexField("Bone", typeof(Vector4i), VertexFieldSemantics.Bone, 32, VertexFieldMarshalType.Int32, 4)]
[VertexField("Weight", typeof(Vector4), VertexFieldSemantics.Weight, 48, VertexFieldMarshalType.Float, 4)]
[VertexField("TextureIndex", typeof(int), VertexFieldSemantics.TextureIndex, 64, VertexFieldMarshalType.Int32, 1)]
[VertexField("Tangent", typeof(Vector3), VertexFieldSemantics.Tangent, 68, VertexFieldMarshalType.Float, 3)]
internal partial struct SkinnedTangentVertex
{
}
