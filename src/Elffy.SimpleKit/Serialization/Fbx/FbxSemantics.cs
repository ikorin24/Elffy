#nullable enable
using System;
using System.Threading;
using FbxTools;
using Elffy.Effective.Unsafes;
using Elffy.Core;
using Elffy.Effective;

namespace Elffy.Serialization.Fbx
{
    internal static class FbxSemantics
    {
        public static void GetMesh(FbxObject fbx, out UnsafeRawList<Vertex> vertices, out UnsafeRawList<int> indices, CancellationToken cancellationToken)
        {
            //ref readonly var globalSetting = ref fbx.Find(FbxConstStrings.GlobalSettings());

            // [root] --|-- "Objects"  ---------|---- "Geometry"
            //          |                       |---- "Geometry"
            //          |-- "GlobalSettings"    |---- "Geometry"
            //                                  |----  ...


            vertices = default;
            indices = default;
            try {
                vertices = UnsafeRawList<Vertex>.New(1024);
                indices = UnsafeRawList<int>.New(1024);

                ref readonly var objects = ref fbx.Find(FbxConstStrings.Objects());

                using var buf = new UnsafeRawArray<int>(objects.Children.Length);
                var geometryCount = objects.FindIndexAll(FbxConstStrings.Geometry(), buf.AsSpan());
                foreach(var i in buf.AsSpan(0, geometryCount)) {
                    cancellationToken.ThrowIfCancellationRequested();

                    ref readonly var geometry = ref objects.Children[i];
                    var props = geometry.Properties;
                    var isMeshGeometry = (props.Length >= 3) &&
                                         (props[2].Type == FbxPropertyType.String) &&
                                         props[2].AsString().SequenceEqual(FbxConstStrings.Mesh());
                    if(isMeshGeometry == false) { continue; }

                    GetGeometryMesh(geometry, out var meshGeometry);
                    ResolveMesh(meshGeometry, indices, vertices);
                }
            }
            catch {
                vertices.Dispose();
                indices.Dispose();
                throw;
            }
        }

        private static void GetGeometryMesh(in FbxNode geometry, out MeshGeometry meshGeometry)
        {
            // - "Geometry"     --------+--- "Vertices"
            //   ([0]:long ID,          |    ([0]:double[] Positions)
            //    [1]:string Name,      |
            //    [2]:string = "Mesh")  |--- "PolygonVertexIndex"
            //                          |    ([0]:int[] IndicesRaw)
            //                          |
            //                          |--- "LayerElementNormal" ---+--- "Normals"
            //                          |                            |    ([0]:double[] Normals)
            //                          |                            |
            //                          |                            |--- "NormalIndex"
            //                          |                            |    ([0]:int[] NormalIndices; if ReferenceInformationType == "IndexToDirect")
            //                          |                            |
            //                          |                            |
            //                          |                            |--- "MappingInformationType"
            //                          |                            |    ([0]:string "ByVertice" or "ByPolygonVertex")
            //                          |                            |
            //                          |                            |--- "ReferenceInformationType"
            //                          |                                 ([0]:string "Direct" or "IndexToDirect")
            //                          |
            //                          |
            //                          |--- "LayerElementMaterial" ----- "Materials"
            //                                                            ([0]:int[] Materials)


            meshGeometry = default;
            meshGeometry.ID = geometry.Properties[0].AsInt64();
            meshGeometry.Name = geometry.Properties[1].AsString();

            foreach(var node in geometry.Children) {
                var nodeName = node.Name;
                if(nodeName.SequenceEqual(FbxConstStrings.Vertices())) {
                    // "Vertices"

                    meshGeometry.Positions = node.Properties[0].AsDoubleArray();
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.PolygonVertexIndex())) {
                    // "PolygonVertexindex"

                    meshGeometry.IndicesRaw = node.Properties[0].AsInt32Array();
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.LayerElementNormal())) {
                    // "LayerElementNormal"

                    meshGeometry.Normals = node.Find(FbxConstStrings.Normals()).Properties[0].AsDoubleArray();

                    var referenceType = node.Find(FbxConstStrings.ReferenceInformationType()).Properties[0].AsString();
                    var mappingType = node.Find(FbxConstStrings.MappingInformationType()).Properties[0].AsString();

                    meshGeometry.NormalReferenceType = GetReferenceInformationType(referenceType);
                    meshGeometry.NormalMappingType = GetMappingInformationType(mappingType);

                    if(meshGeometry.NormalReferenceType == ReferenceInformationType.IndexToDirect) {
                        meshGeometry.NormalIndices = node.Find(FbxConstStrings.NormalIndex()).Properties[0].AsInt32Array();
                    }
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.LayerElementMaterial())) {
                    // "LayerElementMaterial"

                    meshGeometry.Materials = node.Find(FbxConstStrings.Materials()).Properties[0].AsInt32Array();
                }
            }
        }

        private static void ResolveMesh(in MeshGeometry meshGeometry, UnsafeRawList<int> indicesMarged, UnsafeRawList<Vertex> verticesMarged)
        {
            var normalMappingType = meshGeometry.NormalMappingType;
            var indicesOffset = indicesMarged.Count;
            var indicesRaw = meshGeometry.IndicesRaw;
            var positions = meshGeometry.Positions.MarshalCast<double, VecD3>();
            var normals = meshGeometry.Normals.MarshalCast<double, VecD3>();
            var normalIndices = meshGeometry.NormalIndices;
            var normalRefType = meshGeometry.NormalReferenceType;

            switch(normalMappingType) {
                case MappingInformationType.ByVertice: {
                    if(positions.Length != normals.Length) { throw new FormatException(); }
                    break;
                }
                case MappingInformationType.ByPolygonVertex:
                    if(indicesRaw.Length != normals.Length) { throw new FormatException(); }
                    break;
                default:
                    throw new FormatException();
            }

            int n_gon = 0;
            for(int i = 0; i < indicesRaw.Length; i++) {
                n_gon++;
                var isLast = indicesRaw[i] < 0;
                var index = isLast ? (-indicesRaw[i] - 1) : indicesRaw[i];     // 負のインデックスは多角形ポリゴンの最後の頂点を表す (2の補数が元の値)

                var normalIndex = normalMappingType == MappingInformationType.ByVertice ? index : i;
                if(normalRefType == ReferenceInformationType.IndexToDirect) {
                    normalIndex = normalIndices[normalIndex];
                }

                verticesMarged.Add(new Vertex(
                    position: (Vector3)positions[index],
                    normal: (Vector3)normals[normalIndex],
                    uv: default      // TODO:
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

        private static MappingInformationType GetMappingInformationType(ReadOnlySpan<byte> str)
        {
            if(str.SequenceEqual(FbxConstStrings.ByVertice())) {
                return MappingInformationType.ByVertice;
            }
            else if(str.SequenceEqual(FbxConstStrings.ByPolygonVertex())) {
                return MappingInformationType.ByPolygonVertex;
            }
            else {
                throw new FormatException();
            }
        }

        public static ReferenceInformationType GetReferenceInformationType(ReadOnlySpan<byte> str)
        {
            if(str.SequenceEqual(FbxConstStrings.Direct())) {
                return ReferenceInformationType.Direct;
            }
            else if(str.SequenceEqual(FbxConstStrings.IndexToDirect())) {
                return ReferenceInformationType.IndexToDirect;
            }
            else {
                throw new FormatException();
            }
        }

        public ref struct MeshGeometry
        {
            public long ID;
            public ReadOnlySpan<byte> Name;
            public ReadOnlySpan<int> IndicesRaw;
            public ReadOnlySpan<double> Positions;
            public ReadOnlySpan<double> Normals;
            public ReadOnlySpan<int> NormalIndices;
            public ReadOnlySpan<int> Materials;
            public MappingInformationType NormalMappingType;
            public ReferenceInformationType NormalReferenceType;
        }

        private struct VecD3
        {
            public double X;
            public double Y;
            public double Z;

            public static explicit operator Vector3(in VecD3 vec) => new Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);
        }
    }
}
