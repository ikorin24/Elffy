#nullable enable
using System;
using System.Collections.Generic;
using Elffy.Core;
using Elffy.Effective;

namespace Elffy.Shape
{
    public class Model3D : Renderable
    {
        private bool _disposed;
        private readonly UnmanagedArray<Vertex> _vertexArray;
        private readonly UnmanagedArray<int> _indexArray;

        internal Model3D(IList<Vertex> vertices, IList<int> index)
        {
            _vertexArray = vertices.ToUnmanagedArray();
            _indexArray = index.ToUnmanagedArray();
            Activated += OnActivated;
        }

        private void OnActivated(FrameObject frameObject)
        {
            InitGraphicBuffer(_vertexArray.Ptr, _vertexArray.Length, _indexArray.Ptr, _indexArray.Length);
        }

        protected override void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resource here.
                    _vertexArray.Dispose();
                    _indexArray.Dispose();
                    base.Dispose(disposing);
                }
                _disposed = true;
            }
        }
    }
}
