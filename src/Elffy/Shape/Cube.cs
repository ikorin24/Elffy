using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Elffy.Core;

namespace Elffy.Shape
{
    public class Cube : Renderable
    {
        private static readonly Vertex[] _vertexArray = new Vertex[8]
        {
            new Vertex(new Vector3(-1, 1, 1),   new Vector3(-1, 1, 1),   Color4.White, new Vector2(-1, 1)),
            new Vertex(new Vector3(-1, -1, 1),  new Vector3(-1, -1, 1),  Color4.White, new Vector2(-1, -1)),
            new Vertex(new Vector3(1, -1, 1),   new Vector3(1, 1, 1),    Color4.White, new Vector2(1, -1)),
            new Vertex(new Vector3(1, 1, 1),    new Vector3(1, -1, 1),   Color4.White, new Vector2(1, 1)),
            new Vertex(new Vector3(-1, 1, -1),  new Vector3(-1, 1, -1),  Color4.White, new Vector2(-1, 1)),
            new Vertex(new Vector3(-1, -1, -1), new Vector3(-1, -1, -1), Color4.White, new Vector2(-1, -1)),
            new Vertex(new Vector3(1, -1, -1),  new Vector3(1, -1, -1),  Color4.White, new Vector2(1, -1)),
            new Vertex(new Vector3(1, 1, -1),   new Vector3(1, 1, -1),   Color4.White, new Vector2(1, 1)),
        };
        private static readonly int[] _indexArray = new int[36]
        {
            0, 1, 2, 2, 3, 0,
            0, 4, 5, 5, 1, 0,
            1, 5, 6, 6, 2, 1,
            2, 6, 7, 7, 3, 2,
            3, 7, 4, 4, 0, 3,
            5, 4, 7, 7, 6, 5,
        };

        public Cube()
        {
            Load(_vertexArray, _indexArray);
        }
    }
}
