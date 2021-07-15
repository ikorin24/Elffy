#nullable enable
using Elffy.Effective.Unsafes;
using System;

namespace Elffy.Serialization.Fbx
{
    internal readonly struct ResolvedMesh<TVertex> : IDisposable where TVertex : unmanaged
    {
        private readonly UnsafeRawList<TVertex> _vertices;
        private readonly UnsafeRawList<int> _indices;

        public Span<TVertex> Vertices => _vertices.AsSpan();
        public Span<int> Indices => _indices.AsSpan();

        public int VerticesCount => _vertices.Count;
        public int IndicesCount => _indices.Count;

        private ResolvedMesh(int capacity)
        {
            _vertices = UnsafeRawList<TVertex>.New(capacity);
            _indices = UnsafeRawList<int>.New(capacity);
        }

        public static ResolvedMesh<TVertex> New()
        {
            return new ResolvedMesh<TVertex>(1024);
        }

        public void AddVertex(in TVertex vertex)
        {
            _vertices.Add(vertex);
        }

        public void AddIndex(int index)
        {
            _indices.Add(index);
        }

        public void Dispose()
        {
            _vertices.Dispose();
            _indices.Dispose();
        }
    }

    internal readonly struct ResolvedMeshList<TVertex> : IDisposable where TVertex : unmanaged
    {
        private readonly UnsafeRawList<ResolvedMesh<TVertex>> _meshes;

        public int Count => _meshes.Count;

        private ResolvedMeshList(int capacity)
        {
            _meshes = UnsafeRawList<ResolvedMesh<TVertex>>.New(capacity);
        }

        public static ResolvedMeshList<TVertex> New()
        {
            return new ResolvedMeshList<TVertex>(0);
        }

        public void Add(in ResolvedMesh<TVertex> mesh)
        {
            _meshes.Add(mesh);
        }

        public (UnsafeRawArray<TVertex> vertices, UnsafeRawArray<int> indices) CreateCombined()
        {
            UnsafeRawArray<TVertex> vertices = default;
            UnsafeRawArray<int> indices = default;
            try {
                var indicesCout = 0;
                var verticesCount = 0;
                foreach(var mesh in _meshes.AsSpan()) {
                    indicesCout += mesh.IndicesCount;
                    verticesCount += mesh.VerticesCount;
                }
                vertices = new UnsafeRawArray<TVertex>(verticesCount);
                indices = new UnsafeRawArray<int>(indicesCout);

                var offsetV = 0;
                var offsetI = 0;
                foreach(var mesh in _meshes.AsSpan()) {
                    var sourceV = mesh.Vertices;
                    var destV = vertices.AsSpan(offsetV, sourceV.Length);
                    sourceV.CopyTo(destV);

                    var sourceI = mesh.Indices;
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
    }
}
