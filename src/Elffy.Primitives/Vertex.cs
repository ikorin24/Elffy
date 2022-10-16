#nullable enable
using System.Diagnostics;

namespace Elffy
{
    /// <summary>Vertex struct, which has position, normal, and uv.</summary>
    [DebuggerDisplay("{Position}")]
    [GenerateVertex]
    [VertexField("Position", typeof(Vector3), VertexFieldSemantics.Position, 0, VertexFieldMarshalType.Float, 3)]
    [VertexField("Normal", typeof(Vector3), VertexFieldSemantics.Normal, 12, VertexFieldMarshalType.Float, 3)]
    [VertexField("UV", typeof(Vector2), VertexFieldSemantics.UV, 24, VertexFieldMarshalType.Float, 2)]
    public partial struct Vertex
    {
        public Vertex(float posX, float posY, float posZ, float normalX, float normalY, float normalZ, float uvX, float uvY)
        {
            Position.X = posX;
            Position.Y = posY;
            Position.Z = posZ;
            Normal.X = normalX;
            Normal.Y = normalY;
            Normal.Z = normalZ;
            UV.X = uvX;
            UV.Y = uvY;
        }
    }

    /// <summary>Slim vertex struct, which has position and uv.</summary>
    /// <remarks>If you need "Normal", use <see cref="Vertex"/> instead.</remarks>
    [DebuggerDisplay("{Position}")]
    [GenerateVertex]
    [VertexField("Position", typeof(Vector3), VertexFieldSemantics.Position, 0, VertexFieldMarshalType.Float, 3)]
    [VertexField("UV", typeof(Vector2), VertexFieldSemantics.UV, 12, VertexFieldMarshalType.Float, 2)]
    public partial struct VertexSlim
    {
        public VertexSlim(float posX, float posY, float posZ, float uvX, float uvY)
        {
            Position.X = posX;
            Position.Y = posY;
            Position.Z = posZ;
            UV.X = uvX;
            UV.Y = uvY;
        }
    }

    /// <summary>slim vertex struct, which has position and normal.</summary>
    /// <remarks>If you need "UV", use <see cref="Vertex"/> instead.</remarks>
    [DebuggerDisplay("{Position}")]
    [GenerateVertex]
    [VertexField("Position", typeof(Vector3), VertexFieldSemantics.Position, 0, VertexFieldMarshalType.Float, 3)]
    [VertexField("Normal", typeof(Vector3), VertexFieldSemantics.Normal, 12, VertexFieldMarshalType.Float, 3)]
    public partial struct VertexPosNormal
    {
        public VertexPosNormal(float posX, float posY, float posZ, float normalX, float normalY, float normalZ)
        {
            Position.X = posX;
            Position.Y = posY;
            Position.Z = posZ;
            Normal.X = normalX;
            Normal.Y = normalY;
            Normal.Z = normalZ;
        }
    }

    [DebuggerDisplay("{Position}")]
    [GenerateVertex]
    [VertexField("Position", typeof(Vector3), VertexFieldSemantics.Position, 0, VertexFieldMarshalType.Float, 3)]
    public partial struct VertexPosOnly
    {
        public VertexPosOnly(float posX, float posY, float posZ)
        {
            Position.X = posX;
            Position.Y = posY;
            Position.Z = posZ;
        }
    }


    /// <summary>Skinned vertex struct, which has position, normal, uv, bone, weight, texture-index</summary>
    [DebuggerDisplay("{Position}")]
    [GenerateVertex]
    [VertexField("Position", typeof(Vector3), VertexFieldSemantics.Position, 0, VertexFieldMarshalType.Float, 3)]
    [VertexField("Normal", typeof(Vector3), VertexFieldSemantics.Normal, 12, VertexFieldMarshalType.Float, 3)]
    [VertexField("UV", typeof(Vector2), VertexFieldSemantics.UV, 24, VertexFieldMarshalType.Float, 2)]
    [VertexField("Bone", typeof(Vector4i), VertexFieldSemantics.Bone, 32, VertexFieldMarshalType.Int32, 4)]
    [VertexField("Weight", typeof(Vector4), VertexFieldSemantics.Weight, 48, VertexFieldMarshalType.Float, 4)]
    [VertexField("TextureIndex", typeof(int), VertexFieldSemantics.TextureIndex, 64, VertexFieldMarshalType.Int32, 1)]
    public partial struct SkinnedVertex
    {
    }
}
