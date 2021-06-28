#nullable enable
using System;
using System.Threading;
using FbxTools;
using Elffy.Effective.Unsafes;
using Elffy.Core;
using Elffy.Effective;

namespace Elffy.Serialization.Fbx
{
    internal sealed class FbxSemanticParser
    {
        public static FbxSemantics Parse(IResourceLoader loader, string name, CancellationToken cancellationToken = default)
        {
            UnsafeRawArray<int> indices = default;
            UnsafeRawArray<Vertex> vertices = default;
            UnsafeRawArray<Texture> textures = default;
            FbxObject? fbx = null;
            using var combinedMesh = CombinedMesh.New();
            using var stream = loader.GetStream(name);
            try {
                fbx = FbxParser.Parse(stream);
                using var temporalInfo = new SemanticResolver(fbx);
                var objects = fbx.Find(FbxConstStrings.Objects());
                ReadMesh2(objects, combinedMesh, temporalInfo, cancellationToken);
                (vertices, indices) = combinedMesh.CreateCombined();
                textures = ReadTexture(objects, cancellationToken);
                ReadMaterial(objects);

                return new FbxSemantics(fbx, indices, vertices, textures);
            }
            catch {
                fbx?.Dispose();
                indices.Dispose();
                vertices.Dispose();
                textures.Dispose();
                throw;
            }
        }

        private static void ReadMesh2(FbxNode objects, in CombinedMesh combinedMesh, in SemanticResolver info, CancellationToken cancellationToken)
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
            //          |-- "Connections" --+-- "C" ([0]:string ConnectionType, [1]:long sourceID, [2]:long destID)
            //          |                   |-- "C" ([0]:string ConnectionType, [1]:long sourceID, [2]:long destID)
            //          |                   |-- ...


            // <*1> has translation, rotation, and scaling of the model.
            // They are (double x, double y, double z) = ([4], [5], [6])

            using var buf = new ValueTypeRentMemory<int>(objects.Children.Count);
            var bufSpan = buf.Span;
            var geometryCount = objects.FindIndexAll(FbxConstStrings.Geometry(), bufSpan);
            foreach(var i in bufSpan.Slice(0, geometryCount)) {
                cancellationToken.ThrowIfCancellationRequested();
                var geometryNode = objects.Children[i];
                var props = geometryNode.Properties;
                var isMeshGeometry = (props.Length > 2) && props[2].TryAsString(out var type) && type.SequenceEqual(FbxConstStrings.Mesh());
                if(isMeshGeometry == false) { continue; }
                GetGeometryMesh(geometryNode, out var meshGeometry);
                if(info.TryGetMeshModel(meshGeometry, out var model) == false) { throw new FormatException(); }
                ref var resolved = ref combinedMesh.NewMeshToAdd();
                ResolveMesh2(meshGeometry, resolved, model);
                DeformerOfMesh(meshGeometry, info);
            }
            return;
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

        private static void ResolveMesh2(in MeshGeometry mesh, in CombinedMesh.ResolvedMesh resolved, in MeshModel meshModel)
        {
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

                resolved.AddVertex(new Vertex(
                    position: (Vector3)positions[index] + meshModel.Translation,
                    normal: (Vector3)normals[normalIndex],
                    uv: (Vector2)uv[uvIndex]
                ));
                if(isLast) {
                    if(n_gon <= 2) { throw new FormatException(); }
                    for(int n = 0; n < n_gon - 2; n++) {
                        var j = i - n_gon + 1;
                        resolved.AddIndex(j);
                        resolved.AddIndex(j + n + 1);
                        resolved.AddIndex(j + n + 2);
                    }
                    n_gon = 0;
                }
            }
        }

        private static UnsafeRawArray<Texture> ReadTexture(FbxNode objects, CancellationToken cancellationToken)
        {
            using var buf = new ValueTypeRentMemory<int>(objects.Children.Count);
            var bufSpan = buf.Span;
            var textureCount = objects.FindIndexAll(FbxConstStrings.Texture(), bufSpan);
            var textures = new UnsafeRawArray<Texture>(textureCount);
            try {
                int i = 0;
                foreach(var index in bufSpan.Slice(0, textureCount)) {
                    cancellationToken.ThrowIfCancellationRequested();
                    var textureNode = objects.Children[index];
                    var id = textureNode.Properties[0].AsInt64();
                    var fileName = textureNode.Find(FbxConstStrings.RelativeFilename()).Properties[0].AsString();
                    textures[i] = new Texture(id, fileName);
                    i++;
                }
                return textures;
            }
            catch {
                textures.Dispose();
                throw;
            }
        }

        private static void ReadMaterial(FbxNode objects)
        {
            using var buf = new ValueTypeRentMemory<int>(objects.Children.Count);
            var bufSpan = buf.Span;
            var count = objects.FindIndexAll(FbxConstStrings.Materials(), bufSpan);
            foreach(var index in bufSpan.Slice(0, count)) {
                var material = objects.Children[index];
                var id = material.Properties[0].AsInt64();
                var properties70 = material.Find(FbxConstStrings.Properties70());

                foreach(var child in properties70.Children) {
                    var props = child.Properties;
                    if(props[0].AsString().SequenceEqual(FbxConstStrings.Diffuse())) {
                        // diffuse
                        var v = (Vector3)new VecD3(props[4].AsDouble(), props[5].AsDouble(), props[6].AsDouble());
                        break;
                    }
                }
            }
        }

        private static void DeformerOfMesh(in MeshGeometry meshGeometry, in SemanticResolver info)
        {
            if(info.TryGetSkinDeformer(meshGeometry, out var skin) == false) { return; }
            using var memory = info.GetClusterDeformers(skin, out var count);
            var clusters = memory.Span.Slice(0, count);
            foreach(var cluster in clusters) {
                var indices = cluster.GetIndices();
                var weights = cluster.GetWeights();
                cluster.GetInitialPosition(out var mat);
            }
        }
    }
}
