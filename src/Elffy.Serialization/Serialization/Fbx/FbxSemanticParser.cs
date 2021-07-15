#nullable enable
using System.IO;
using System;
using System.Threading;
using FbxTools;
using Elffy.Effective.Unsafes;
using Elffy.Effective;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Elffy.Serialization.Fbx
{
    internal sealed class FbxSemanticParser<TVertex> where TVertex : unmanaged
    {
        public static FbxSemantics<TVertex> Parse(IResourceLoader loader, string name, CancellationToken cancellationToken = default)
        {
            using var stream = loader.GetStream(name);
            return Parse(stream, cancellationToken);
        }

        public static FbxSemantics<TVertex> Parse(Stream stream, CancellationToken cancellationToken = default)
        {
            var vertexCreator = SkinnedVertexCreator<TVertex>.Build();

            ValueTypeRentMemory<RawString> textures = default;
            var fbx = FbxParser.Parse(stream);
            try {
                using var resolver = new SemanticResolver(fbx);
                var objectsNode = resolver.ObjectsNode;
                var (meshes, skeletons) = ParseMeshAndSkeleton(resolver, vertexCreator, cancellationToken);
                using(meshes)
                using(skeletons) {
                    textures = ParseTexture(objectsNode, cancellationToken);
                    ParseMaterial(objectsNode);
                    var (vertices, indices) = meshes.CreateCombined();
                    return new FbxSemantics<TVertex>(ref fbx, ref indices, ref vertices, ref textures);
                }
            }
            catch {
                fbx?.Dispose();
                textures.Dispose();
                throw;
            }
        }

        private static (ResolvedMeshList<TVertex> meshes, SkeletonDataList skeletons) ParseMeshAndSkeleton(
            in SemanticResolver resolver,
            SkinnedVertexCreator<TVertex> vertexCreator,
            CancellationToken cancellationToken)
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

            var skeletons = new SkeletonDataList(resolver);
            var resolvedMeshes = ResolvedMeshList<TVertex>.New();
            try {
                var objects = resolver.ObjectsNode;
                using var _ = new ValueTypeRentMemory<int>(objects.Children.Count);
                var buf = _.Span;
                var geometryCount = objects.FindIndexAll(FbxConstStrings.Geometry(), buf);
                foreach(var i in buf.Slice(0, geometryCount)) {
                    cancellationToken.ThrowIfCancellationRequested();
                    var geometryNode = objects.Children[i];
                    if(geometryNode.IsMeshGeometry() == false) { continue; }
                    var meshGeometry = new MeshGeometry(geometryNode);
                    if(resolver.TryGetMeshModel(meshGeometry, out var model) == false) { throw new FormatException(); }
                    var resolvedMesh = ResolveMesh(meshGeometry, model, vertexCreator, resolver, skeletons);
                    resolvedMeshes.Add(resolvedMesh);
                }
            }
            catch {
                skeletons.Dispose();
                resolvedMeshes.Dispose();
                throw;
            }
            return (resolvedMeshes, skeletons);
        }

        private static ResolvedMesh<TVertex> ResolveMesh(in MeshGeometry mesh,
                                                         in MeshModel meshModel,
                                                         SkinnedVertexCreator<TVertex> vertexCreator,
                                                         in SemanticResolver resolver,
                                                         in SkeletonDataList skeletons)
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

            var resolvedMesh = ResolvedMesh<TVertex>.New();
            try {
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
                    var v = new TVertex();
                    vertexCreator.PositionOf(ref v) = (Vector3)positions[index] + meshModel.Translation;
                    vertexCreator.NormalOf(ref v) = (Vector3)normals[normalIndex];
                    vertexCreator.UVOf(ref v) = (Vector2)uv[uvIndex];
                    resolvedMesh.AddVertex(v);

                    if(isLast) {
                        if(n_gon <= 2) { throw new FormatException(); }
                        for(int n = 0; n < n_gon - 2; n++) {
                            var j = i - n_gon + 1;
                            resolvedMesh.AddIndex(j);
                            resolvedMesh.AddIndex(j + n + 1);
                            resolvedMesh.AddIndex(j + n + 2);
                        }
                        n_gon = 0;
                    }
                }

                ResolveBone(mesh, resolvedMesh.Vertices, resolver, skeletons, vertexCreator);
                return resolvedMesh;
            }
            catch {
                resolvedMesh.Dispose();
                throw;
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

        private static void ResolveBone(in MeshGeometry meshGeometry,
                                           Span<TVertex> vertices,
                                           in SemanticResolver resolver,
                                           in SkeletonDataList skeletonList,
                                           SkinnedVertexCreator<TVertex> vertexCreator)
        {
            if(resolver.TryGetSkinDeformer(meshGeometry, out var skin) == false) { return; }
            using var _ = resolver.GetClusterDeformers(skin, out var count);
            var clusters = _.AsSpan(0, count);

            var skeletons = skeletonList.Span;
            var skeletonIndex = 0;
            ref readonly var skeleton = ref Unsafe.NullRef<SkeletonData>();

            using var boneCountsMem = new ValueTypeRentMemory<int>(vertices.Length, true);
            var boneCounts = boneCountsMem.Span;

            foreach(var cluster in clusters) {
                if(resolver.TryGetLimb(cluster, out var limb) == false) { throw new FormatException(); }

                int boneIndex;
                if(UnsafeEx.IsNullRefReadOnly(in skeleton)) {
                    if(TryGetSkeletonIndex(skeletonList, limb, out skeletonIndex, out boneIndex) == false) { throw new FormatException(); }
                    skeleton = ref skeletons[skeletonIndex];
                }
                else {
                    if(TryGetBoneIndex(skeleton, limb, out boneIndex) == false) { throw new FormatException(); }
                }

                var indices = cluster.GetIndices().AsSpan();
                var weights = cluster.GetWeights().AsSpan();
                if(indices.Length != weights.Length) {
                    throw new FormatException();
                }

                cluster.GetInitialPosition(out var mat);

                Debug.Assert(indices.Length == weights.Length);
                for(int i = 0; i < indices.Length; i++) {
                    ref readonly var index = ref indices.At(i);
                    ref readonly var weight = ref weights.At(i);
                    ref var offset = ref boneCounts[index];
                    if(offset >= 4) {
                        offset++;
                        continue;       // TODO: 4つ以上のボーンを持つ場合
                        //throw new NotSupportedException("No single vertex can be associated with more than four bones.");
                    }
                    ref var v = ref vertices[index];
                    Unsafe.Add(ref Unsafe.As<Vector4i, int>(ref vertexCreator.BoneOf(ref v)), offset) = boneIndex;
                    Unsafe.Add(ref Unsafe.As<Vector4, float>(ref vertexCreator.WeightOf(ref v)), offset) = (float)weight;
                    offset++;
                }
            }
            return;

            static bool TryGetSkeletonIndex(in SkeletonDataList skeletonList, in LimbNode limb, out int skeletonIndex, out int boneIndex)
            {
                var skeletons = skeletonList.Span;
                for(int i = 0; i < skeletons.Length; i++) {
                    if(TryGetBoneIndex(skeletons[i], limb, out boneIndex)) {
                        skeletonIndex = i;
                        return true;
                    }
                }
                skeletonIndex = -1;
                boneIndex = -1;
                return false;
            }

            static bool TryGetBoneIndex(in SkeletonData skeleton, in LimbNode limb, out int index)
            {
                var bones = skeleton.Bones;
                for(int i = 0; i < bones.Length; i++) {
                    if(bones[i].LimbNode.ID == limb.ID) {
                        index = i;
                        return true;
                    }
                }
                index = -1;
                return false;
            }

        }
    }
}
