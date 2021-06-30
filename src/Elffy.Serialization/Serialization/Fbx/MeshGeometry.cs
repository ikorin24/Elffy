#nullable enable
using Elffy.Core;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using FbxTools;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Serialization.Fbx
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

    internal readonly ref struct MeshGeometry
    {
        public readonly long ID;
        public readonly RawString Name;
        private readonly RawArray<int> _indicesRaw;
        private readonly RawArray<double> _positions;
        private readonly RawArray<double> _normals;
        private readonly RawArray<int> _normalIndices;
        public readonly MappingInformationType NormalMappingType;
        public readonly ReferenceInformationType NormalReferenceType;
        private readonly RawArray<double> _uv;
        private readonly RawArray<int> _uvIndices;
        public readonly MappingInformationType UVMappingType;
        public readonly ReferenceInformationType UVReferenceType;
        public readonly RawArray<int> Materials;

        public ReadOnlySpan<int> IndicesRaw => _indicesRaw.AsSpan();
        public ReadOnlySpan<VecD3> Positions => _positions.AsSpan().MarshalCast<double, VecD3>();
        public ReadOnlySpan<VecD3> Normals => _normals.AsSpan().MarshalCast<double, VecD3>();
        public ReadOnlySpan<int> NormalIndices => _normalIndices.AsSpan();
        public ReadOnlySpan<VecD2> UV => _uv.AsSpan().MarshalCast<double, VecD2>();
        public ReadOnlySpan<int> UVIndices => _uvIndices.AsSpan();

        public MeshGeometry(FbxNode geometryNode)
        {
            Debug.Assert(geometryNode.IsMeshGeometry());

            ID = geometryNode.Properties[0].AsInt64();
            Name = geometryNode.Properties[1].AsString();

            _positions = default;
            _indicesRaw = default;
            _normals = default;
            NormalReferenceType = default;
            NormalMappingType = default;
            _normalIndices = default;
            _uv = default;
            UVReferenceType = default;
            UVMappingType = default;
            _uvIndices = default;
            Materials = default;
            foreach(var node in geometryNode.Children) {
                var nodeName = node.Name;
                if(nodeName.SequenceEqual(FbxConstStrings.Vertices())) {
                    // "Vertices"

                    _positions = node.Properties[0].AsDoubleArray();
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.PolygonVertexIndex())) {
                    // "PolygonVertexindex"

                    _indicesRaw = node.Properties[0].AsInt32Array();
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.LayerElementNormal())) {
                    // "LayerElementNormal"

                    _normals = node.Find(FbxConstStrings.Normals()).Properties[0].AsDoubleArray();

                    var referenceType = node.Find(FbxConstStrings.ReferenceInformationType()).Properties[0].AsString();
                    var mappingType = node.Find(FbxConstStrings.MappingInformationType()).Properties[0].AsString();

                    NormalReferenceType = referenceType.ToReferenceInformationType();
                    NormalMappingType = mappingType.ToMappingInformationType();

                    if(NormalReferenceType == ReferenceInformationType.IndexToDirect) {
                        _normalIndices = node.Find(FbxConstStrings.NormalIndex()).Properties[0].AsInt32Array();
                    }
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.LayerElementUV())) {
                    // "LayerElementUV"

                    _uv = node.Find(FbxConstStrings.UV()).Properties[0].AsDoubleArray();
                    var referenceType = node.Find(FbxConstStrings.ReferenceInformationType()).Properties[0].AsString();
                    var mappingType = node.Find(FbxConstStrings.MappingInformationType()).Properties[0].AsString();
                    UVReferenceType = referenceType.ToReferenceInformationType();
                    UVMappingType = mappingType.ToMappingInformationType();

                    if(UVReferenceType == ReferenceInformationType.IndexToDirect) {
                        _uvIndices = node.Find(FbxConstStrings.UVIndex()).Properties[0].AsInt32Array();
                    }
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.LayerElementMaterial())) {
                    // "LayerElementMaterial"

                    Materials = node.Find(FbxConstStrings.Materials()).Properties[0].AsInt32Array();
                }
            }
        }
    }

    internal readonly struct CombinedMesh : IDisposable     // Don't use default struct
    {
        private readonly UnsafeRawList<ResolvedMeshPrivate> _meshes;

        public int Count => _meshes.Count;

        private CombinedMesh(int capacity)
        {
            _meshes = UnsafeRawList<ResolvedMeshPrivate>.New(capacity);
        }

        public static CombinedMesh New()
        {
            return new CombinedMesh(0);
        }

        public ref ResolvedMesh NewMeshToAdd()
        {
            _meshes.Add(ResolvedMeshPrivate.New());
            ref var mesh = ref _meshes[_meshes.Count - 1];
            unsafe {
                Debug.Assert(sizeof(ResolvedMesh) == sizeof(ResolvedMeshPrivate));
            }
            return ref Unsafe.As<ResolvedMeshPrivate, ResolvedMesh>(ref mesh);
        }

        public (UnsafeRawArray<Vertex> vertices, UnsafeRawArray<int> indices) CreateCombined()
        {
            UnsafeRawArray<Vertex> vertices = default;
            UnsafeRawArray<int> indices = default;
            try {
                var indicesCout = 0;
                var verticesCount = 0;
                foreach(var mesh in _meshes.AsSpan()) {
                    indicesCout += mesh.Indices.Count;
                    verticesCount += mesh.Vertices.Count;
                }
                vertices = new UnsafeRawArray<Vertex>(verticesCount);
                indices = new UnsafeRawArray<int>(indicesCout);

                var offsetV = 0;
                var offsetI = 0;
                foreach(var mesh in _meshes.AsSpan()) {
                    var sourceV = mesh.Vertices.AsSpan();
                    var destV = vertices.AsSpan(offsetV, sourceV.Length);
                    sourceV.CopyTo(destV);

                    var sourceI = mesh.Indices.AsSpan();
                    var destI = indices.AsSpan(offsetI, sourceI.Length);
                    sourceI.CopyTo(destI);
                    for(int i = 0; i < destI.Length; i++) {
                        destI[i] += offsetV;
                    }

                    offsetV += sourceV.Length;
                    offsetI += sourceI.Length;
                }

                return (vertices, indices);
            }
            catch {
                vertices.Dispose();
                indices.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            foreach(var mesh in _meshes.AsSpan()) {
                mesh.Dispose();
            }
            _meshes.Dispose();
        }

        private readonly struct ResolvedMeshPrivate     // Don't use default struct
        {
            public readonly UnsafeRawList<Vertex> Vertices;
            public readonly UnsafeRawList<int> Indices;

            private ResolvedMeshPrivate(UnsafeRawList<Vertex> v, UnsafeRawList<int> i)
            {
                Vertices = v;
                Indices = i;
            }

            public static ResolvedMeshPrivate New()
            {
                const int InitialCapacity = 1024;
                return new ResolvedMeshPrivate(UnsafeRawList<Vertex>.New(InitialCapacity), UnsafeRawList<int>.New(InitialCapacity));
            }

            public void Dispose()
            {
                Vertices.Dispose();
                Indices.Dispose();
            }
        }

        internal readonly struct ResolvedMesh    // Don't use default struct
        {
            private readonly ResolvedMeshPrivate _mesh;

            public ReadOnlySpan<Vertex> Vertices => _mesh.Vertices.AsSpan();

            public ReadOnlySpan<int> Indices => _mesh.Indices.AsSpan();

            public void AddVertex(in Vertex v)
            {
                _mesh.Vertices.Add(v);
            }

            public void AddIndex(int index)
            {
                _mesh.Indices.Add(index);
            }
        }
    }
}
