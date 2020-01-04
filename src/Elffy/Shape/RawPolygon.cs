#nullable enable
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using System;

namespace Elffy.Shape
{
    public class RawPolygon : Renderable
    {
        private bool _disposed;

        private readonly UnmanagedArray<Vertex> _vertexArray;
        private readonly UnmanagedArray<int> _indexArray;

        public RawPolygon(Vertex[] vertexArray, int[] indexArray) : this(vertexArray.AsSpan(), indexArray.AsSpan()) { }

        public RawPolygon(UnmanagedArray<Vertex> vertexArray, UnmanagedArray<int> indexArray) : this(vertexArray.AsSpan(), indexArray.AsSpan()) { }

        public RawPolygon(Span<Vertex> vertexArray, Span<int> indexArray)
        {
            ArgumentChecker.ThrowArgumentIf(vertexArray.Length != indexArray.Length, $"Length of {nameof(vertexArray)} is not same as {nameof(vertexArray)}.");
            Activated += OnActivated;
            _vertexArray = new UnmanagedArray<Vertex>(vertexArray);
            _indexArray = new UnmanagedArray<int>(indexArray);
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
                    base.Dispose(true);
                }
                _disposed = true;
            }
            base.Dispose();
        }
    }
}
