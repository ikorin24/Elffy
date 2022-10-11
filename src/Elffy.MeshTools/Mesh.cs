#nullable enable
using System;

namespace Elffy
{
    public unsafe sealed class Mesh : IDisposable
    {
        private IMeshData _meshData;

        public uint VertexSize => _meshData.VertexSize;
        public uint IndexSize => _meshData.IndexSize;
        public ulong VertexCount => _meshData.VertexCount;
        public ulong IndexCount => _meshData.IndexCount;

        internal Mesh(in MeshContent meshContent, ref UniquePtr disposableResource)
        {
            _meshData = new MeshContainerData(meshContent, ref disposableResource);
        }

        private Mesh(IMeshData meshData)
        {
            _meshData = meshData;
        }

        public static Mesh Create(VertexTypeData vertexTypeData, uint indexSize,
                                  void* vertices, ulong verticesByteLength, void* indices, ulong indicesByteLength,
                                  ref UniquePtr data, Action<UniquePtr> onMeshDisposed)
        {
            var meshData = new RenderableMeshData(vertexTypeData, indexSize, vertices, verticesByteLength, indices, indicesByteLength, ref data, onMeshDisposed);
            return new Mesh(meshData);
        }

        //public TVertex* GetVertices<TVertex>(out ulong vertexCount) where TVertex : unmanaged, IVertex
        //{
        //    if(sizeof(TVertex) != _vertexSize) {
        //        ThrowVertexTypeMismatched();
        //        static void ThrowVertexTypeMismatched() => throw new ArgumentException("Vertex size is mismatched.");
        //    }
        //    vertexCount = _vertexCount;
        //    return (TVertex*)_vertices;
        //}

        //public TIndex* GetIndices<TIndex>(out ulong indexCount) where TIndex : unmanaged
        //{
        //    if(sizeof(TIndex) != _indexSize) {
        //        ThrowIndexTypeMismatched();
        //        static void ThrowIndexTypeMismatched() => throw new ArgumentException("Index size is mismatched.");
        //    }
        //    indexCount = _indexCount;
        //    return (TIndex*)_indices;
        //}

        public void Dispose()
        {
            _meshData.Dispose();
        }

        private interface IMeshData : IDisposable
        {
            uint VertexSize { get; }
            uint IndexSize { get; }
            ulong VertexCount { get; }
            ulong IndexCount { get; }
        }

        private sealed class MeshContainerData : IMeshData
        {
            private uint _vertexSize;
            private uint _indexSize;
            private uint _vertexFieldCount;
            private VertexFieldInfo* _vertexFields;
            private void* _vertices;
            private ulong _verticesByteLength;
            private void* _indices;
            private ulong _indicesByteLength;
            private UniquePtr _disposableResource;

            private ulong _vertexCount;
            private ulong _indexCount;

            public uint VertexSize => _vertexSize;
            public uint IndexSize => _indexSize;
            public ulong VertexCount => _vertexCount;
            public ulong IndexCount => _indexCount;

            public MeshContainerData(in MeshContent meshContent, ref UniquePtr disposableResource)
            {
                _vertexSize = meshContent.VertexSize;
                _indexSize = meshContent.IndexSize;
                _vertexFieldCount = meshContent.VertexFieldCount;
                _vertexFields = meshContent.VertexFields;
                _vertices = meshContent.Vertices;
                _verticesByteLength = meshContent.VerticesByteLength;
                _indices = meshContent.Indices;
                _indicesByteLength = meshContent.IndicesByteLength;
                _disposableResource = disposableResource.Move();

                _vertexCount = _verticesByteLength / _vertexSize;
                _indexCount = _indicesByteLength / _indexSize;
            }

            ~MeshContainerData() => Dispose(false);

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                Dispose(true);
            }

            private void Dispose(bool disposing)
            {
                _disposableResource.Dispose();
            }
        }

        private sealed class RenderableMeshData : IMeshData
        {
            private readonly VertexTypeData _vertexTypeData;

            private readonly uint _vertexSize;
            private readonly ulong _vertexCount;
            private readonly uint _indexSize;
            private readonly ulong _indexCount;
            private readonly void* _vertices;
            private readonly ulong _verticesByteLength;
            private readonly void* _indices;
            private readonly ulong _indicesByteLength;
            private UniquePtr _data;
            private readonly Action<UniquePtr> _onMeshDisposed;

            public uint VertexSize => _vertexSize;

            public uint IndexSize => _indexSize;

            public ulong VertexCount => _vertexCount;

            public ulong IndexCount => _indexCount;

            public RenderableMeshData(VertexTypeData vertexTypeData, uint indexSize,
                                      void* vertices, ulong verticesByteLength, void* indices, ulong indicesByteLength,
                                      ref UniquePtr data, Action<UniquePtr> onMeshDisposed)
            {
                _vertexTypeData = vertexTypeData;
                _vertexSize = (uint)vertexTypeData.VertexSize;
                _vertexCount = verticesByteLength / _vertexSize;
                _indexSize = indexSize;
                _indexCount = indicesByteLength / _indexSize;
                _vertices = vertices;
                _verticesByteLength = verticesByteLength;
                _indices = indices;
                _indicesByteLength = indicesByteLength;
                _data = data.Move();
                _onMeshDisposed = onMeshDisposed;
            }

            ~RenderableMeshData() => Dispose(false);

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                Dispose(true);
            }

            private void Dispose(bool disposing)
            {
                _onMeshDisposed(_data.Move());
            }
        }
    }
}
