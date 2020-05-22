#nullable enable
using System;
using System.Collections.Generic;
using Elffy.Core;
using Elffy.Effective;
using UnmanageUtility;

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
        }

        public ReadOnlySpan<Vertex> GetVertexArray() => _vertexArray.AsSpan();

        public ReadOnlySpan<int> GetIndexArray() => _indexArray.AsSpan();

        public void UpdateVertex(ReadOnlySpan<Vertex> vertexArray, ReadOnlySpan<int> indexArray)
        {
            LoadGraphicBuffer(vertexArray, indexArray);
        }

        protected override void OnAlive()
        {
            base.OnAlive();
            LoadGraphicBuffer(_vertexArray.AsSpan(), _indexArray.AsSpan());
        }

        protected override void OnDead()
        {
            base.OnDead();
            _vertexArray.Dispose();
            _indexArray.Dispose();
        }
    }
}
