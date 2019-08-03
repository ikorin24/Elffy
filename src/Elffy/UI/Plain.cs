using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy;
using Elffy.Core;
using OpenTK;
using OpenTK.Graphics;
using System.Runtime.InteropServices;

namespace Elffy.UI
{
    public class Plain : Renderable
    {
        private static readonly Vertex[] _vertexArray = new Vertex[4]
        {
            new Vertex(new Vector3(-1, 1, 0),  new Vector3(0, 0, 1), new Vector2(0, 1)),
            new Vertex(new Vector3(-1, -1, 0), new Vector3(0, 0, 1), new Vector2(0, 0)),
            new Vertex(new Vector3(1, -1, 0),  new Vector3(0, 0, 1), new Vector2(1, 0)),
            new Vertex(new Vector3(1, 1, 0),   new Vector3(0, 0, 1), new Vector2(1, 1)),
        };
        private static readonly int[] _indexArray = new int[6] { 0, 1, 2, 2, 3, 0 };

        protected override void OnActivated()
        {
            base.OnActivated();
            InitGraphicBuffer(_vertexArray, _indexArray);
        }
    }
}
