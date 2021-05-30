#nullable enable
using System;
using System.Threading;
using System.IO;
using FbxTools;
using Elffy.Effective.Unsafes;
using Elffy.Core;
using Elffy.Effective;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Elffy.Serialization.Fbx
{
    internal sealed class FbxSemantics : IDisposable
    {
        private FbxObject _fbx;
        private UnsafeRawList<MeshGeometry> _meshes = default;
        private UnsafeRawList<int> _indices;
        private UnsafeRawList<Vertex> _vertices;

        private ConnectionList Connections => new ConnectionList(_fbx.Find(FbxConstStrings.Connections()));

        public ReadOnlySpan<int> Indices => _indices.AsSpan();

        public ReadOnlySpan<Vertex> Vertices => _vertices.AsSpan();

        private FbxSemantics(FbxObject fbx)
        {
            _fbx = fbx;
        }

        ~FbxSemantics() => Dispose(false);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            var fbx = _fbx;
            if(fbx is null) { return; }

            // Release unmanaged resources
            _meshes.Dispose();
            _indices.Dispose();
            _vertices.Dispose();

            if(disposing) {
                // Release managed resources if IDisposable.Dispose() called
                fbx.Dispose();
                Unsafe.AsRef(_fbx) = null!;
            }
        }

        public static FbxSemantics Parse(Stream stream, CancellationToken cancellationToken = default)
        {
            FbxSemantics? semantics = null;
            try {
                var fbx = FbxParser.Parse(stream);
                semantics = new FbxSemantics(fbx);
                semantics.ReadMesh(cancellationToken);

                var connections = semantics.Connections;
                // TODO: resolve connections

                return semantics;
            }
            catch {
                semantics?.Dispose();
                throw;
            }
        }


        private void ReadMesh(CancellationToken cancellationToken)
        {
            // [root] --+-- "Objects"  ------+---- "Geometry"
            //          |                    |---- "Geometry"
            //          |                    |---- "Geometry"
            //          |                    |----  ...
            //          |                    |                                        <*1>
            //          |                    |---- "Model" ----- "Properties70" --+-- "P" ([0]:string propTypeName, ...)
            //          |                    |                                    |-- "P" ([0]:string propTypeName, ...)
            //          |                    |                                    |-- ...
            //          |                    |---- "Model" ---- ...
            //          |                    |---- ...
            //          |                   
            //          |
            //          |-- "GlobalSettings"
            //          | 
            //          |-- "Connections"


            // <*1> has translation, rotation, and scaling of the model.
            // They are (double x, double y, double z) = ([4], [5], [6])

            Debug.Assert(_vertices.IsNull);
            Debug.Assert(_indices.IsNull);
            try {
                _vertices = UnsafeRawList<Vertex>.New(1024);
                _indices = UnsafeRawList<int>.New(1024);

                var objects = _fbx.Find(FbxConstStrings.Objects());

                using var buf = new UnsafeRawArray<int>(objects.Children.Count);
                var geometryCount = objects.FindIndexAll(FbxConstStrings.Geometry(), buf.AsSpan());
                foreach(var i in buf.AsSpan(0, geometryCount)) {
                    cancellationToken.ThrowIfCancellationRequested();

                    var geometry = objects.Children[i];
                    var props = geometry.Properties;
                    var isMeshGeometry = (props.Length >= 3) &&
                                         (props[2].Type == FbxPropertyType.String) &&
                                         props[2].AsString().SequenceEqual(FbxConstStrings.Mesh());
                    if(isMeshGeometry == false) { continue; }

                    GetGeometryMesh(geometry, out var meshGeometry);
                    ResolveMesh(meshGeometry, _indices, _vertices);
                }


                var modelCount = objects.FindIndexAll(FbxConstStrings.Model(), buf.AsSpan());
                foreach(var i in buf.AsSpan(0, modelCount)) {
                    cancellationToken.ThrowIfCancellationRequested();
                    var model = objects.Children[i];
                    var property70 = model.Find(FbxConstStrings.Properties70());

                    foreach(var p in property70.Children) {
                        var props = p.Properties;
                        if(props.Length < 1 || props[0].Type != FbxPropertyType.String) { continue; }
                        var propTypeName = props[0].AsString();

                        if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Translation())) {
                            var translation = new Vector3(
                                (float)props[4].AsDouble(),
                                (float)props[5].AsDouble(),
                                (float)props[6].AsDouble());
                        }
                        else if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Rotation())) {
                            var rotation = new Vector3(
                                (float)props[4].AsDouble(),
                                (float)props[5].AsDouble(),
                                (float)props[6].AsDouble());
                        }
                        else if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Scaling())) {
                            var scale = new Vector3(
                                (float)props[4].AsDouble(),
                                (float)props[5].AsDouble(),
                                (float)props[6].AsDouble());
                        }
                    }
                }
                return;
            }
            catch {
                _vertices.Dispose();
                _indices.Dispose();
                throw;
            }
        }

        private static void GetGeometryMesh(in FbxNode geometry, out MeshGeometry mesh)
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


            mesh = default;
            mesh.ID = geometry.Properties[0].AsInt64();
            mesh.Name = geometry.Properties[1].AsString();

            foreach(var node in geometry.Children) {
                var nodeName = node.Name;
                if(nodeName.SequenceEqual(FbxConstStrings.Vertices())) {
                    // "Vertices"

                    mesh.Positions = node.Properties[0].AsDoubleArray();
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.PolygonVertexIndex())) {
                    // "PolygonVertexindex"

                    mesh.IndicesRaw = node.Properties[0].AsInt32Array();
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.LayerElementNormal())) {
                    // "LayerElementNormal"

                    mesh.Normals = node.Find(FbxConstStrings.Normals()).Properties[0].AsDoubleArray();

                    var referenceType = node.Find(FbxConstStrings.ReferenceInformationType()).Properties[0].AsString();
                    var mappingType = node.Find(FbxConstStrings.MappingInformationType()).Properties[0].AsString();

                    mesh.NormalReferenceType = referenceType.ToReferenceInformationType();
                    mesh.NormalMappingType = mappingType.ToMappingInformationType();

                    if(mesh.NormalReferenceType == ReferenceInformationType.IndexToDirect) {
                        mesh.NormalIndices = node.Find(FbxConstStrings.NormalIndex()).Properties[0].AsInt32Array();
                    }
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.LayerElementUV())) {
                    // "LayerElementUV"

                    mesh.UV = node.Find(FbxConstStrings.UV()).Properties[0].AsDoubleArray();
                    var referenceType = node.Find(FbxConstStrings.ReferenceInformationType()).Properties[0].AsString();
                    var mappingType = node.Find(FbxConstStrings.MappingInformationType()).Properties[0].AsString();
                    mesh.UVReferenceType = referenceType.ToReferenceInformationType();
                    mesh.UVMappingType = mappingType.ToMappingInformationType();

                    if(mesh.UVReferenceType == ReferenceInformationType.IndexToDirect) {
                        mesh.UVIndices = node.Find(FbxConstStrings.UVIndex()).Properties[0].AsInt32Array();
                    }
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.LayerElementMaterial())) {
                    // "LayerElementMaterial"

                    mesh.Materials = node.Find(FbxConstStrings.Materials()).Properties[0].AsInt32Array();
                }
            }
        }

        private static void ResolveMesh(MeshGeometry mesh, UnsafeRawList<int> indicesMarged, UnsafeRawList<Vertex> verticesMarged)
        {
            var indicesOffset = indicesMarged.Count;
            var positions = mesh.Positions.AsSpan().MarshalCast<double, VecD3>();
            var normals = mesh.Normals.AsSpan().MarshalCast<double, VecD3>();
            var uv = mesh.UV.AsSpan().MarshalCast<double, VecD2>();

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
    }
}
