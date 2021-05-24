#nullable enable
using System;
using StringLiteral;

namespace Elffy.Serialization.Fbx
{
    internal static partial class FbxConstStrings
    {
        [Utf8("GlobalSettings")]
        public static partial ReadOnlySpan<byte> GlobalSettings();

        [Utf8("Objects")]
        public static partial ReadOnlySpan<byte> Objects();

        [Utf8("Geometry")]
        public static partial ReadOnlySpan<byte> Geometry();

        [Utf8("Vertices")]
        public static partial ReadOnlySpan<byte> Vertices();

        [Utf8("PolygonVertexIndex")]
        public static partial ReadOnlySpan<byte> PolygonVertexIndex();

        [Utf8("LayerElementNormal")]
        public static partial ReadOnlySpan<byte> LayerElementNormal();

        [Utf8("LayerElementUV")]
        public static partial ReadOnlySpan<byte> LayerElementUV();

        [Utf8("UV")]
        public static partial ReadOnlySpan<byte> UV();

        [Utf8("UVIndex")]
        public static partial ReadOnlySpan<byte> UVIndex();

        [Utf8("Normals")]
        public static partial ReadOnlySpan<byte> Normals();

        [Utf8("NormalIndex")]
        public static partial ReadOnlySpan<byte> NormalIndex();

        [Utf8("MappingInformationType")]
        public static partial ReadOnlySpan<byte> MappingInformationType();

        [Utf8("ByVertice")]
        public static partial ReadOnlySpan<byte> ByVertice();

        [Utf8("ByPolygonVertex")]
        public static partial ReadOnlySpan<byte> ByPolygonVertex();

        [Utf8("ByControllPoint")]
        public static partial ReadOnlySpan<byte> ByControllPoint();

        [Utf8("Direct")]
        public static partial ReadOnlySpan<byte> Direct();

        [Utf8("IndexToDirect")]
        public static partial ReadOnlySpan<byte> IndexToDirect();

        [Utf8("ReferenceInformationType")]
        public static partial ReadOnlySpan<byte> ReferenceInformationType();

        [Utf8("LayerElementMaterial")]
        public static partial ReadOnlySpan<byte> LayerElementMaterial();

        [Utf8("Materials")]
        public static partial ReadOnlySpan<byte> Materials();

        [Utf8("Mesh")]
        public static partial ReadOnlySpan<byte> Mesh();
    }
}
