#nullable enable
using System.IO;
using System;
using System.Linq;
using System.Threading;
using FbxTools;
using Elffy.Effective.Unsafes;
using Elffy.Core;
using Elffy.Effective;
using System.Collections.Generic;

namespace Elffy.Serialization.Fbx
{
    internal sealed class FbxSemanticParser
    {
        public static FbxSemantics Parse(IResourceLoader loader, string name, CancellationToken cancellationToken = default)
        {
            using var stream = loader.GetStream(name);
            return Parse(stream, cancellationToken);
        }

        public static FbxSemantics Parse(Stream stream, CancellationToken cancellationToken = default)
        {
            UnsafeRawArray<int> indices = default;
            UnsafeRawArray<SkinnedVertex> vertices = default;
            ValueTypeRentMemory<RawString> textures = default;
            FbxObject? fbx = null;
            try {
                fbx = FbxParser.Parse(stream);
                using var resolver = new SemanticResolver(fbx);
                using var models = new SemanticModelList(resolver);

                var objectsNode = fbx.Find(FbxConstStrings.Objects());
                (vertices, indices) = ParseMesh(objectsNode, resolver, cancellationToken);
                textures = ParseTexture(objectsNode, cancellationToken);
                ParseMaterial(objectsNode);


                // Model(LimbNode) のソースが、子のボーンModel(LimbNode)
                // 子供からは遡れないので注意
                // Model(Null)がルート

                return new FbxSemantics(fbx, indices, vertices, ref textures);
            }
            catch {
                fbx?.Dispose();
                indices.Dispose();
                vertices.Dispose();
                textures.Dispose();
                throw;
            }
        }

        private static (UnsafeRawArray<SkinnedVertex> vertices, UnsafeRawArray<int> indices) ParseMesh(FbxNode objects, in SemanticResolver resolver, CancellationToken cancellationToken)
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

            using var combinedMesh = CombinedMesh.New();
            using var buf = new ValueTypeRentMemory<int>(objects.Children.Count);
            var bufSpan = buf.Span;
            var geometryCount = objects.FindIndexAll(FbxConstStrings.Geometry(), bufSpan);
            foreach(var i in bufSpan.Slice(0, geometryCount)) {
                cancellationToken.ThrowIfCancellationRequested();
                var geometryNode = objects.Children[i];
                if(geometryNode.IsMeshGeometry() == false) { continue; }
                var meshGeometry = new MeshGeometry(geometryNode);
                if(resolver.TryGetMeshModel(meshGeometry, out var model) == false) { throw new FormatException(); }
                ResolveMesh(meshGeometry, model, combinedMesh.NewMeshToAdd());
                DeformerOfMesh(meshGeometry, resolver);
            }
            return combinedMesh.CreateCombined();
        }

        private static void ResolveMesh(in MeshGeometry mesh, in MeshModel meshModel, in CombinedMesh.ResolvedMesh resolved)
        {
            var positions = mesh.Positions;
            var normals = mesh.Normals;
            var uv = mesh.UV;
            var indicesRaw = mesh.IndicesRaw;
            var normalIndices = mesh.NormalIndices;
            var uvIndices = mesh.UVIndices;

            switch(mesh.NormalMappingType) {
                case MappingInformationType.ByPolygonVertex: {
                    if(normals.Length != indicesRaw.Length) { throw new FormatException(); }
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
            for(int i = 0; i < indicesRaw.Length; i++) {
                n_gon++;
                var isLast = indicesRaw[i] < 0;
                var index = isLast ? (-indicesRaw[i] - 1) : indicesRaw[i];     // 負のインデックスは多角形ポリゴンの最後の頂点を表す (2の補数が元の値)

                var normalIndex = mesh.NormalMappingType == MappingInformationType.ByPolygonVertex ? i : index;
                if(mesh.NormalReferenceType == ReferenceInformationType.IndexToDirect) {
                    normalIndex = normalIndices[normalIndex];
                }

                var uvIndex = mesh.UVMappingType == MappingInformationType.ByPolygonVertex ? i : index;
                if(mesh.UVReferenceType == ReferenceInformationType.IndexToDirect) {
                    uvIndex = uvIndices[uvIndex];
                }

                //resolved.AddVertex(new Vertex(
                //    position: (Vector3)positions[index] + meshModel.Translation,
                //    normal: (Vector3)normals[normalIndex],
                //    uv: (Vector2)uv[uvIndex]
                //));
                resolved.AddVertex(new SkinnedVertex(
                    position: (Vector3)positions[index] + meshModel.Translation,
                    normal: (Vector3)normals[normalIndex],
                    uv: (Vector2)uv[uvIndex],
                    bone: default,
                    weight: default,
                    textureIndex: default
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

        private static ValueTypeRentMemory<RawString> ParseTexture(FbxNode objects, CancellationToken cancellationToken)
        {
            using var buf = new ValueTypeRentMemory<int>(objects.Children.Count);
            var bufSpan = buf.Span;
            var textureCount = objects.FindIndexAll(FbxConstStrings.Texture(), bufSpan);
            var textureNames = new ValueTypeRentMemory<RawString>(textureCount);
            var span = textureNames.Span;
            try {
                int i = 0;
                foreach(var index in bufSpan.Slice(0, textureCount)) {
                    cancellationToken.ThrowIfCancellationRequested();
                    var textureNode = objects.Children[index];
                    //var id = textureNode.Properties[0].AsInt64();
                    var fileName = textureNode.Find(FbxConstStrings.RelativeFilename()).Properties[0].AsString();
                    span[i] = fileName;
                    i++;
                }
                return textureNames;
            }
            catch {
                textureNames.Dispose();
                throw;
            }
        }

        private static void ParseMaterial(FbxNode objects)
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

        private static void DeformerOfMesh(in MeshGeometry meshGeometry, in SemanticResolver resolver)
        {
            if(resolver.TryGetSkinDeformer(meshGeometry, out var skin) == false) { return; }
            using var memory = resolver.GetClusterDeformers(skin, out var count);
            var clusters = memory.Span.Slice(0, count);
            foreach(var cluster in clusters) {
                var indices = cluster.GetIndices();
                var weights = cluster.GetWeights();
                cluster.GetInitialPosition(out var mat);
            }
            return;
        }
    }
}
