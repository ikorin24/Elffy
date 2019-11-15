#nullable enable
using Elffy.Core;

namespace Elffy.Shape
{
    public class Model3D : Renderable
    {
        private readonly Vertex[] _vertexArray;
        private readonly int[] _indexArray;

        internal Model3D(Vertex[] vertices, int[] index)
        {
            _vertexArray = vertices;
            _indexArray = index;
            Activated += OnActivated;
        }

        private void OnActivated(FrameObject frameObject)
        {
            InitGraphicBuffer(_vertexArray, _indexArray);
        }
    }
}
