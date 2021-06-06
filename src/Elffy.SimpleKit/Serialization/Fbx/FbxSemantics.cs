#nullable enable
using System;
using System.Threading;
using System.Linq;
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
        private UnsafeRawArray<Texture> _textures;
        private UnsafeRawList<int> _indices;
        private UnsafeRawList<Vertex> _vertices;
        private UnsafeRawList<Bone> _bones;

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
            _indices.Dispose();
            _vertices.Dispose();
            _textures.Dispose();
            _bones.Dispose();

            if(disposing) {
                // Release managed resources if IDisposable.Dispose() called
                fbx.Dispose();
                Unsafe.AsRef(_fbx) = null!;
            }
        }

        public static FbxSemantics Parse(IResourceLoader loader, string name, CancellationToken cancellationToken = default)
        {
            FbxSemantics? semantics = null;
            ParserTemporalInfo temporalInfo = default;
            try {
                using var stream = loader.GetStream(name);
                var fbx = FbxParser.Parse(stream);
                semantics = new FbxSemantics(fbx);

                var objects = fbx.Find(FbxConstStrings.Objects());
                using var buf = new UnsafeRawArray<int>(objects.Children.Count);
                var bufSpan = buf.AsSpan();
                ReadMesh(objects, bufSpan, ref semantics._vertices, ref semantics._indices, ref temporalInfo.Meshes, cancellationToken);
                ReadTexture(objects, bufSpan, ref semantics._textures, cancellationToken);
                ReadModel(objects, bufSpan, ref temporalInfo.Models, cancellationToken);
                ReadMaterial(objects, bufSpan);
                ReadDeformer(objects, bufSpan, ref semantics._bones, cancellationToken);
                CreateDic(ref temporalInfo);

                var connections = new ConnectionList(fbx.Find(FbxConstStrings.Connections()));
                Connect(connections, temporalInfo, semantics._vertices.AsSpan(), cancellationToken);
                return semantics;
            }
            catch {
                temporalInfo.Dispose();
                semantics?.Dispose();
                throw;
            }
        }

        private static void ReadDeformer(FbxNode objects, Span<int> indexBuf, ref UnsafeRawList<Bone> bones, CancellationToken cancellationToken)
        {
            Debug.Assert(bones.IsNull);
            bones = UnsafeRawList<Bone>.New();
            var deformerCount = objects.FindIndexAll(FbxConstStrings.Deformer(), indexBuf);
            foreach(var index in indexBuf.Slice(0, deformerCount)) {
                cancellationToken.ThrowIfCancellationRequested();
                var deformer = objects.Children[index];
                var props = deformer.Properties;
                var id = props[0].AsInt64();
                var name = props[1].AsString();
                if(props.Length <= 2 || props[2].AsString().SequenceEqual(FbxConstStrings.Cluster()) == false) {
                    continue;
                }
                Bone bone;
                bone.ID = id;
                var array = deformer.Find(FbxConstStrings.TransformLink()).Properties[0].AsDoubleArray();
                GetMatrix(array, out bone.InitPosition);
                bones.Add(bone);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetMatrix(RawArray<double> array, out Matrix4 mat)
        {
            mat.M00 = (float)array[0];
            mat.M10 = (float)array[1];
            mat.M20 = (float)array[2];
            mat.M30 = (float)array[3];
            mat.M01 = (float)array[4];
            mat.M11 = (float)array[5];
            mat.M21 = (float)array[6];
            mat.M31 = (float)array[7];
            mat.M02 = (float)array[8];
            mat.M12 = (float)array[9];
            mat.M22 = (float)array[10];
            mat.M32 = (float)array[11];
            mat.M03 = (float)array[12];
            mat.M13 = (float)array[13];
            mat.M23 = (float)array[14];
            mat.M33 = (float)array[15];
        }

        private static void ReadMesh(FbxNode objects,
                                     Span<int> indexBuf,
                                     ref UnsafeRawList<Vertex> vertices,
                                     ref UnsafeRawList<int> indices,
                                     ref UnsafeRawList<MeshGeometry> meshes,
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

            Debug.Assert(vertices.IsNull);
            Debug.Assert(indices.IsNull);
            Debug.Assert(meshes.IsNull);
            Debug.Assert(indexBuf.Length >= objects.Children.Count);

            vertices = UnsafeRawList<Vertex>.New(1024);
            indices = UnsafeRawList<int>.New(1024);
            meshes = UnsafeRawList<MeshGeometry>.New(128);

            var geometryCount = objects.FindIndexAll(FbxConstStrings.Geometry(), indexBuf);
            foreach(var i in indexBuf.Slice(0, geometryCount)) {
                cancellationToken.ThrowIfCancellationRequested();

                var geometry = objects.Children[i];
                var props = geometry.Properties;
                var isMeshGeometry = (props.Length >= 3) &&
                                     (props[2].Type == FbxPropertyType.String) &&
                                     props[2].AsString().SequenceEqual(FbxConstStrings.Mesh());
                if(isMeshGeometry == false) { continue; }

                GetGeometryMesh(geometry, out var meshGeometry);
                ResolveMesh(meshGeometry, indices, vertices, out meshGeometry.ResolvedIndicesRange, out meshGeometry.ResolvedVerticesRange);
                meshes.Add(meshGeometry);
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

        private static void ResolveMesh(MeshGeometry mesh, UnsafeRawList<int> indicesMarged, UnsafeRawList<Vertex> verticesMarged,
                                        out Range indicesRange, out Range verticesRange)
        {
            var indicesOffset = indicesMarged.Count;
            var verticesOffset = verticesMarged.Count;
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
                        var j = verticesOffset + i - n_gon + 1;
                        indicesMarged.Add(j);
                        indicesMarged.Add(j + n + 1);
                        indicesMarged.Add(j + n + 2);
                    }
                    n_gon = 0;
                }
            }

            indicesRange = new Range(indicesOffset, indicesMarged.Count);
            verticesRange = new Range(verticesOffset, verticesMarged.Count);
        }


        private static void ReadTexture(FbxNode objects,
                                        Span<int> indexBuf,
                                        ref UnsafeRawArray<Texture> textures,
                                        CancellationToken cancellationToken)
        {
            Debug.Assert(textures.IsEmpty);

            var textureCount = objects.FindIndexAll(FbxConstStrings.Texture(), indexBuf);
            textures = new UnsafeRawArray<Texture>(textureCount);
            int i = 0;
            foreach(var index in indexBuf.Slice(0, textureCount)) {
                cancellationToken.ThrowIfCancellationRequested();
                var textureNode = objects.Children[index];
                var id = textureNode.Properties[0].AsInt64();
                var fileName = textureNode.Find(FbxConstStrings.RelativeFilename()).Properties[0].AsString();
                textures[i] = new Texture(id, fileName);
                i++;
            }
            return;
        }

        private static void ReadModel(FbxNode objects, Span<int> indexBuf, ref UnsafeRawList<Model> models, CancellationToken cancellationToken)
        {
            Debug.Assert(models.IsNull);
            models = UnsafeRawList<Model>.New(128);

            var modelCount = objects.FindIndexAll(FbxConstStrings.Model(), indexBuf);
            foreach(var i in indexBuf.Slice(0, modelCount)) {
                cancellationToken.ThrowIfCancellationRequested();
                var modelNode = objects.Children[i];
                var id = modelNode.Properties[0].AsInt64();
                var property70 = modelNode.Find(FbxConstStrings.Properties70());


                var translation = new Vector3();
                var rotation = new Vector3();
                var scale = new Vector3(1f, 1f, 1f);
                foreach(var p in property70.Children) {
                    var props = p.Properties;
                    if(props.Length < 1 || props[0].Type != FbxPropertyType.String) { continue; }
                    var propTypeName = props[0].AsString();

                    if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Translation())) {
                        translation = new Vector3(
                            (float)props[4].AsDouble(),
                            (float)props[5].AsDouble(),
                            (float)props[6].AsDouble());
                    }
                    else if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Rotation())) {
                        rotation = new Vector3(
                            (float)props[4].AsDouble(),
                            (float)props[5].AsDouble(),
                            (float)props[6].AsDouble());
                    }
                    else if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Scaling())) {
                        scale = new Vector3(
                            (float)props[4].AsDouble(),
                            (float)props[5].AsDouble(),
                            (float)props[6].AsDouble());
                    }
                }
                models.Add(new Model(id, translation, rotation, scale));
            }
        }

        private static void ReadMaterial(FbxNode objects, Span<int> indexBuf)
        {
            var count = objects.FindIndexAll(FbxConstStrings.Materials(), indexBuf);
            foreach(var index in indexBuf.Slice(0, count)) {
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

        private static void Connect(ConnectionList connections, in ParserTemporalInfo temporalInfo, Span<Vertex> vertices, CancellationToken cancellationToken)
        {
            var objDic = temporalInfo.ObjectDic;
            var models = temporalInfo.Models.AsSpan();
            var meshes = temporalInfo.Meshes.AsSpan();
            foreach(var connect in connections) {
                cancellationToken.ThrowIfCancellationRequested();
                switch(connect.ConnectionType) {
                    case ConnectionType.OO: {
                        if(!objDic.TryGetValue(connect.SourceID, out var src) || !objDic.TryGetValue(connect.DestID, out var dest)) {
                            break;
                        }

                        switch((src.Type, dest.Type)) {
                            case (ObjectType.MeshGeometry, ObjectType.Model): {
                                ref readonly var mesh = ref meshes[src.Index];
                                ref readonly var model = ref models[dest.Index];
                                // TODO: Scale, Rotation
                                var translation = model.Translation;
                                var meshVertices = vertices[mesh.ResolvedVerticesRange];
                                for(int i = 0; i < meshVertices.Length; i++) {
                                    meshVertices[i].Position += translation;
                                }

                                break;
                            }
                            default: { break; }
                        }
                        break;
                    }
                    case ConnectionType.OP: {
                        // I don't use this connection so far.
                        break;
                    }
                    default: {
                        break;
                    }
                }
            }
        }

        private static void CreateDic(ref ParserTemporalInfo temporalInfo)
        {
            //var dic = InternalDictionary<long, ObjectInfo>.New();
            var dic = new System.Collections.Generic.Dictionary<long, ObjectInfo>();
            temporalInfo.ObjectDic = dic;

            var meshes = temporalInfo.Meshes.AsSpan();
            for(int i = 0; i < meshes.Length; i++) {
                dic.Add(meshes[i].ID, new ObjectInfo(ObjectType.MeshGeometry, i));
            }

            var models = temporalInfo.Models.AsSpan();
            for(int i = 0; i < models.Length; i++) {
                dic.Add(models[i].ID, new ObjectInfo(ObjectType.Model, i));
            }
        }


        private ref struct ParserTemporalInfo
        {
            public UnsafeRawList<MeshGeometry> Meshes;
            public UnsafeRawList<Model> Models;

            //public InternalDictionary<long, ObjectInfo> ObjectDic;
            public System.Collections.Generic.Dictionary<long, ObjectInfo> ObjectDic;

            public void Dispose()
            {
                Meshes.Dispose();
                Models.Dispose();
                //ObjectDic.Dispose();
            }
        }
    }
}
