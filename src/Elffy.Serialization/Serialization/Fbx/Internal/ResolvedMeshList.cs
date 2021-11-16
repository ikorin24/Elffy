#nullable enable
using Elffy.Effective.Unsafes;
using System;

namespace Elffy.Serialization.Fbx.Internal
{
    internal readonly struct ResolvedMeshList<TVertex> : IDisposable where TVertex : unmanaged
    {
        private readonly UnsafeRawList<ResolvedMesh<TVertex>> _meshes;

        public int Count => _meshes.Count;

        private ResolvedMeshList(int capacity)
        {
            _meshes = new UnsafeRawList<ResolvedMesh<TVertex>>(capacity);
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
            // In many cases, the number of vertices is quite large and `ValueTypeRentMemory` does not work well,
            // so use `UnsafeRawArray`.
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
