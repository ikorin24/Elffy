#nullable enable
using Elffy.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Shapes
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
            throw new NotImplementedException();
        }

        protected override void OnAlive()
        {
            base.OnAlive();
            LoadGraphicBuffer(_vertexArray.Span, _indexArray.Span);
        }
    }
}
