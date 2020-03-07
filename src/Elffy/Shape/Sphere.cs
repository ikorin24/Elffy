#nullable enable
using Elffy.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Shape
{
    public class Sphere : Renderable
    {
        private static readonly ReadOnlyMemory<Vertex> _vertexArray;
        private static readonly ReadOnlyMemory<int> _indexArray;

        static Sphere()
        {
            // TODO:
            _vertexArray = new ReadOnlyMemory<Vertex>();
            _indexArray = new ReadOnlyMemory<int>();
        }

        public Sphere()
        {
            Activated += OnActivated;
            throw new NotImplementedException();
        }

        private void OnActivated(FrameObject frameObject)
        {
            LoadGraphicBuffer(_vertexArray.Span, _indexArray.Span);
        }
    }
}
