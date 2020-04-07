#nullable enable
using System;
using System.Collections.Generic;
using Elffy.Core;
using Elffy.Effective;

namespace Elffy.Shape
{
    public class Model3D : Renderable
    {
        private readonly UnmanagedArray<Vertex> _vertexArray;
        private readonly UnmanagedArray<int> _indexArray;

        internal Model3D(ReadOnlySpan<Vertex> vertexArray, ReadOnlySpan<int> indexArray)
        {
            _vertexArray = vertexArray.ToUnmanagedArray();
            _indexArray = indexArray.ToUnmanagedArray();
            Activated += OnActivated;
            Terminated += OnTerminated;
        }

        public ReadOnlySpan<Vertex> GetVertexArray() => _vertexArray.AsSpan();

        public ReadOnlySpan<int> GetIndexArray() => _indexArray.AsSpan();

        public void UpdateVertex(ReadOnlySpan<Vertex> vertexArray, ReadOnlySpan<int> indexArray)
        {
            LoadGraphicBuffer(vertexArray, indexArray);
        }

        private void OnActivated(FrameObject frameObject)
        {
            LoadGraphicBuffer(_vertexArray.Ptr, _vertexArray.Length, _indexArray.Ptr, _indexArray.Length);
        }

        private void OnTerminated(FrameObject frameObject)
        {
            _vertexArray.Dispose();
            _indexArray.Dispose();
        }
    }
}
