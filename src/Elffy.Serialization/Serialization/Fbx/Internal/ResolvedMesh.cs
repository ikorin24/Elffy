#nullable enable
using Elffy.AssemblyServices;
using Elffy.Effective.Unsafes;
using System;

namespace Elffy.Serialization.Fbx.Internal
{
    [DontUseDefault]
    internal readonly struct ResolvedMesh<TVertex> : IDisposable where TVertex : unmanaged, IVertex
    {
        private readonly UnsafeRawList<TVertex> _vertices;
        private readonly UnsafeRawList<int> _indices;

        public Span<TVertex> Vertices => _vertices.AsSpan();
        public Span<int> Indices => _indices.AsSpan();

        public int VerticesCount => _vertices.Count;
        public int IndicesCount => _indices.Count;

        public ResolvedMesh()
        {
            const int capacity = 1024;
            _vertices = new UnsafeRawList<TVertex>(capacity);
            _indices = new UnsafeRawList<int>(capacity);
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
}
