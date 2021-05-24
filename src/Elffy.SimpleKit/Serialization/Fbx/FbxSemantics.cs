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
            //                          |--- "LayerElementUV" -------+--- "UV"
            //                          |                            |    ([0]:double[] UV)
            //                          |                            |
            //                          |                            |--- "UVIndex"
            //                          |                            |    ([0]:int[] UVIndices; i ReferenceInformationType == "IndexToDirect")
            //                          |                            |
            //                          |                            |--- "MappingInformationType"
            //                          |                            |    ([0]:string "ByControllPoint" or "ByPolygonVertex")
            //                          |                            |
            //                          |                            |--- "ReferenceInformationType"
            //                          |                                 ([0]:string "Direct" or "IndexToDirect")
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
                else if(nodeName.SequenceEqual(FbxConstStrings.LayerElementUV())) {
                    // "LayerElementUV"

                    meshGeometry.UV = node.Find(FbxConstStrings.UV()).Properties[0].AsDoubleArray();
                    var referenceType = node.Find(FbxConstStrings.ReferenceInformationType()).Properties[0].AsString();
                    var mappingType = node.Find(FbxConstStrings.MappingInformationType()).Properties[0].AsString();
                    meshGeometry.UVReferenceType = GetReferenceInformationType(referenceType);
                    meshGeometry.UVMappingType = GetMappingInformationType(mappingType);

                    if(meshGeometry.UVReferenceType == ReferenceInformationType.IndexToDirect) {
                        meshGeometry.UVIndices = node.Find(FbxConstStrings.UVIndex()).Properties[0].AsInt32Array();
                    }
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.LayerElementMaterial())) {
                    // "LayerElementMaterial"

                    meshGeometry.Materials = node.Find(FbxConstStrings.Materials()).Properties[0].AsInt32Array();
                }
            }
        }

        private static void ResolveMesh(MeshGeometry mesh, UnsafeRawList<int> indicesMarged, UnsafeRawList<Vertex> verticesMarged)
        {
            var indicesOffset = indicesMarged.Count;
            var positions = mesh.Positions.MarshalCast<double, VecD3>();
            var normals = mesh.Normals.MarshalCast<double, VecD3>();
            var uv = mesh.UV.MarshalCast<double, VecD2>();

            switch(mesh.NormalMappingType) {
                case MappingInformationType.ByPolygonVertex: {
                    if(normals.Length != mesh.IndicesRaw.Length) { throw new FormatException(); }
                    break;
                }
                case MappingInformationType.ByVertice: {
                    if(normals.Length != positions.Length) { throw new FormatException(); }
                    break;
                }
                default:
                    throw new FormatException();
            }

            //switch(mesh.UVMappingType) {
            //    case MappingInformationType.ByPolygonVertex: {
            //        if(uv.Length != mesh.IndicesRaw.Length) { throw new FormatException(); }
            //        break;
            //    }
            //    case MappingInformationType.ByControllPoint: {
            //        if(uv.Length != positions.Length) { throw new FormatException(); }
            //        break;
            //    }
            //    default:
            //        throw new FormatException();
            //}

            int n_gon = 0;
            for(int i = 0; i < mesh.IndicesRaw.Length; i++) {
                n_gon++;
                var isLast = mesh.IndicesRaw[i] < 0;
                var index = isLast ? (-mesh.IndicesRaw[i] - 1) : mesh.IndicesRaw[i];     // 負のインデックスは多角形ポリゴンの最後の頂点を表す (2の補数が元の値)

                var normalIndex = mesh.NormalMappingType == MappingInformationType.ByPolygonVertex ? i : index;
                if(mesh.NormalReferenceType == ReferenceInformationType.IndexToDirect) {
                    normalIndex = mesh.NormalIndices[normalIndex];
                }

                var uvIndex = mesh.UVMappingType == MappingInformationType.ByPolygonVertex ? i : index;
                if(mesh.UVReferenceType == ReferenceInformationType.IndexToDirect) {
                    uvIndex = mesh.UVIndices[uvIndex];
                }

                verticesMarged.Add(new Vertex(
                    position: (Vector3)positions[index],
                    normal: (Vector3)normals[normalIndex],
                    uv: (Vector2)uv[uvIndex]
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
            else if(str.SequenceEqual(FbxConstStrings.ByPolygonVertex())) {
                return MappingInformationType.ByControllPoint;
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
            public MappingInformationType NormalMappingType;
            public ReferenceInformationType NormalReferenceType;
            public ReadOnlySpan<double> UV;
            public ReadOnlySpan<int> UVIndices;
            public MappingInformationType UVMappingType;
            public ReferenceInformationType UVReferenceType;
            public ReadOnlySpan<int> Materials;
        }

        private struct VecD3
        {
#pragma warning disable 0649    // The field is never assigned to, and will always have its default value.
            public double X;
            public double Y;
            public double Z;
#pragma warning restore 0649

            public static explicit operator Vector3(in VecD3 vec) => new Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);
        }

        private struct VecD2
        {
#pragma warning disable 0649    // The field is never assigned to, and will always have its default value.
            public double X;
            public double Y;
#pragma warning restore 0649

            public static explicit operator Vector2(in VecD2 vec) => new Vector2((float)vec.X, (float)vec.Y);
        }
    }
}
