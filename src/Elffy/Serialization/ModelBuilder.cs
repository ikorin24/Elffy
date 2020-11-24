#nullable enable
using System;
using System.Linq;
using System.IO;
using Elffy.Core;
using StringLiteral;
using UnmanageUtility;

namespace Elffy.Serialization
{
    public static class ModelBuilder
    {
        public static void BuildFromFbx(Stream stream, out UnmanagedList<Vertex> vertices, out UnmanagedList<int> indices)
        {
            using var fbx = FbxTools.FbxParser.Parse(stream);

            ref readonly var objects = ref fbx.Find(FbxConsts.Objects());

            vertices = null!;
            indices = null!;

            Span<int> buf = stackalloc int[objects.Children.Length];
            foreach(var i in buf.Slice(0, objects.FindIndexAll(FbxConsts.Geometry(), buf))) {
                GeometryNode(objects.Children[i],
                             out var id,
                             out var name,
                             out var positions,
                             out var indicesRaw,
                             out var normals,
                             out var materials);

                vertices ??= new UnmanagedList<Vertex>(indicesRaw.Length);
                indices ??= new UnmanagedList<int>(indicesRaw.Length);
                ResolveVertices(indicesRaw, positions, normals, vertices, indices);
            }
            vertices ??= new UnmanagedList<Vertex>(0);
            indices ??= new UnmanagedList<int>(0);
        }

        private static void ResolveVertices(ReadOnlySpan<int> indicesRaw,
                                            ReadOnlySpan<double> positions,
                                            ReadOnlySpan<double> normals,
                                            UnmanagedList<Vertex> verticesMarged,
                                            UnmanagedList<int> indicesMarged)
        {
            // positions の数は幾何的な頂点の数
            // normals の数は属性が同じ頂点の数 (属性: 座標・法線・頂点色など) (== indices の数)
            // 頂点属性の数になるように、positions を拡張する

            var indicesOffset = indicesMarged.Count;
            int n_gon = 0;
            for(int i = 0; i < indicesRaw.Length; i++) {
                n_gon++;
                var isLast = indicesRaw[i] < 0;
                var index = isLast ? (-indicesRaw[i] - 1) : indicesRaw[i];     // 負のインデックスは多角形ポリゴンの最後の頂点を表す (2の補数が元の値)

                var p = new Vector3((float)positions[index * 3], (float)positions[index * 3 + 1], (float)positions[index * 3 + 2]);
                var normal = new Vector3((float)normals[i * 3], (float)normals[i * 3 + 1], (float)normals[i * 3 + 2]);
                //verticesMarged.Add(new RigVertex(
                //    position: p,
                //    normal: normal,
                //    texcoord: default,      // TODO:
                //    bone: default,          // TODO:
                //    weight: Vector4.UnitX   // TODO:
                //));
                verticesMarged.Add(new Vertex(
                    position: p,
                    normal: normal,
                    texcoord: default      // TODO:
                ));
                if(isLast) {
                    if(n_gon <= 2) { throw new FormatException(); }
                    for(int n = 0; n < n_gon - 2; n++) {
                        var j = indicesOffset + i - n_gon + 1;
                        indicesMarged.Add(j);
                        indicesMarged.Add(j + n + 1);
                        indicesMarged.Add(j + n + 2);
                    }
                    n_gon = 0;
                }
            }
        }

        private static void GeometryNode(in FbxTools.FbxNode geometry,
                                         out long id,
                                         out ReadOnlySpan<byte> name,
                                         out ReadOnlySpan<double> positions,
                                         out ReadOnlySpan<int> indicesRaw,
                                         out ReadOnlySpan<double> normals,
                                         out ReadOnlySpan<int> materials)
        {
            id = geometry.Properties[0].AsInt64();
            name = geometry.Properties[1].AsString();

            positions = default;
            indicesRaw = default;
            normals = default;
            materials = default;

            foreach(var node in geometry.Children) {
                var nodeName = node.Name;
                if(nodeName.SequenceEqual(FbxConsts.Vertices())) {
                    positions = node.Properties[0].AsDoubleArray();
                }
                else if(nodeName.SequenceEqual(FbxConsts.Indices())) {
                    indicesRaw = node.Properties[0].AsInt32Array();
                }
                else if(nodeName.SequenceEqual(FbxConsts.NormalInfo())) {
                    normals = node.Find(FbxConsts.Normals()).Properties[0].AsDoubleArray();
                }
                else if(nodeName.SequenceEqual(FbxConsts.MaterialInfo())) {
                    materials = node.Find(FbxConsts.Materials()).Properties[0].AsInt32Array();
                }
            }
        }
    }

    internal static partial class FbxConsts
    {
        [Utf8("Objects")]
        public static partial ReadOnlySpan<byte> Objects();

        [Utf8("Geometry")]
        public static partial ReadOnlySpan<byte> Geometry();

        [Utf8("Vertices")]
        public static partial ReadOnlySpan<byte> Vertices();

        [Utf8("PolygonVertexIndex")]
        public static partial ReadOnlySpan<byte> Indices();

        [Utf8("LayerElementNormal")]
        public static partial ReadOnlySpan<byte> NormalInfo();

        [Utf8("Normals")]
        public static partial ReadOnlySpan<byte> Normals();

        [Utf8("LayerElementMaterial")]
        public static partial ReadOnlySpan<byte> MaterialInfo();

        [Utf8("Materials")]
        public static partial ReadOnlySpan<byte> Materials();
    }
}
