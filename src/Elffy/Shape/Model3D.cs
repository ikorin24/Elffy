using Elffy.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            InitGraphicBuffer(_vertexArray, _indexArray);
        }
    }
}
