#nullable enable
using Elffy.Core;
using Elffy.Effective.Unsafes;
using FbxTools;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Serialization.Fbx
{
    internal struct MeshGeometry
    {
        public long ID;
        public RawString Name;
        public RawArray<int> IndicesRaw;
        public RawArray<double> Positions;
        public RawArray<double> Normals;
        public RawArray<int> NormalIndices;
        public MappingInformationType NormalMappingType;
        public ReferenceInformationType NormalReferenceType;
        public RawArray<double> UV;
        public RawArray<int> UVIndices;
        public MappingInformationType UVMappingType;
        public ReferenceInformationType UVReferenceType;
        public RawArray<int> Materials;

        public Range ResolvedIndicesRange;
        public Range ResolvedVerticesRange;
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
            ref var mesh =  ref _meshes[_meshes.Count - 1];
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
