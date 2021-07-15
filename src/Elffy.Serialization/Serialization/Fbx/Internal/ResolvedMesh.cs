#nullable enable
using Elffy.Effective.Unsafes;
using System;

namespace Elffy.Serialization.Fbx.Internal
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
}
