using Elffy.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Shapes
{
    public class Cylinder : Renderable
    {
        private static readonly ReadOnlyMemory<Vertex> _vertexArray;
        private static readonly ReadOnlyMemory<int> _indexArray;

        static Cylinder()
        {
            _vertexArray = new ReadOnlyMemory<Vertex>();
            _indexArray = new ReadOnlyMemory<int>();
        }

        public Cylinder()
        {
            throw new NotImplementedException();
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            LoadGraphicBuffer(_vertexArray.Span, _indexArray.Span);
        }
    }
}
