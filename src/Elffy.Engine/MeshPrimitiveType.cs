#nullable enable
using OpenTK.Graphics.OpenGL4;

namespace Elffy
{
    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue("Points", (int)PrimitiveType.Points)]
    [EnumLikeValue("Lines", (int)PrimitiveType.Lines)]
    [EnumLikeValue("LineLoop", (int)PrimitiveType.LineLoop)]
    [EnumLikeValue("LineStrip", (int)PrimitiveType.LineStrip)]
    [EnumLikeValue("Triangles", (int)PrimitiveType.Triangles)]
    [EnumLikeValue("TriangleStrip", (int)PrimitiveType.TriangleStrip)]
    [EnumLikeValue("TriangleFan", (int)PrimitiveType.TriangleFan)]
    [EnumLikeValue("LinesAdjacency", (int)PrimitiveType.LinesAdjacency)]
    [EnumLikeValue("LineStripAdjacency", (int)PrimitiveType.LineStripAdjacency)]
    [EnumLikeValue("TrianglesAdjacency", (int)PrimitiveType.TrianglesAdjacency)]
    [EnumLikeValue("TriangleStripAdjacency", (int)PrimitiveType.TriangleStripAdjacency)]
    public readonly partial struct MeshPrimitiveType
    {
        internal PrimitiveType ToGLPrimitiveType() => (PrimitiveType)_value;
    }
}
