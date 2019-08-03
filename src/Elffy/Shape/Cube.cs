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
        private const float zero = 0.0f;
        private const float one = 1 / 3.0f;
        private const float two = 2 / 3.0f;
        private const float three = 1.0f;

        private static readonly Vertex[] _vertexArray = new Vertex[36] {
            new Vertex(new Vector3(-1, -1, -1), new Vector3(-1, 0, 0), new Vector2(zero, three)),
            new Vertex(new Vector3(-1, -1, 1),  new Vector3(-1, 0, 0), new Vector2(zero, two)),
            new Vertex(new Vector3(-1, 1, 1),   new Vector3(-1, 0, 0), new Vector2(one, two)),
            new Vertex(new Vector3(1, 1, -1),   new Vector3(0, 0, -1), new Vector2(three, three)),
            new Vertex(new Vector3(-1, -1, -1), new Vector3(0, 0, -1), new Vector2(two, two)),
            new Vertex(new Vector3(-1, 1, -1),  new Vector3(0, 0, -1), new Vector2(three, two)),
            new Vertex(new Vector3(1, -1, 1),   new Vector3(0, -1, 0), new Vector2(two, two)),
            new Vertex(new Vector3(-1, -1, -1), new Vector3(0, -1, 0), new Vector2(one, one)),
            new Vertex(new Vector3(1, -1, -1),  new Vector3(0, -1, 0), new Vector2(two, one)),
            new Vertex(new Vector3(1, 1, -1),   new Vector3(0, 0, -1), new Vector2(three, three)),
            new Vertex(new Vector3(1, -1, -1),  new Vector3(0, 0, -1), new Vector2(two, three)),
            new Vertex(new Vector3(-1, -1, -1), new Vector3(0, 0, -1), new Vector2(two, two)),
            new Vertex(new Vector3(-1, -1, -1), new Vector3(-1, 0, 0), new Vector2(zero, three)),
            new Vertex(new Vector3(-1, 1, 1),   new Vector3(-1, 0, 0), new Vector2(one, two)),
            new Vertex(new Vector3(-1, 1, -1),  new Vector3(-1, 0, 0), new Vector2(one, three)),
            new Vertex(new Vector3(1, -1, 1),   new Vector3(0, -1, 0), new Vector2(two, two)),
            new Vertex(new Vector3(-1, -1, 1),  new Vector3(0, -1, 0), new Vector2(one, two)),
            new Vertex(new Vector3(-1, -1, -1), new Vector3(0, -1, 0), new Vector2(one, one)),
            new Vertex(new Vector3(-1, 1, 1),   new Vector3(0, 0, 1),  new Vector2(three, one)),
            new Vertex(new Vector3(-1, -1, 1),  new Vector3(0, 0, 1),  new Vector2(three, two)),
            new Vertex(new Vector3(1, -1, 1),   new Vector3(0, 0, 1),  new Vector2(two, two)),
            new Vertex(new Vector3(1, 1, 1),    new Vector3(1, 0, 0),  new Vector2(two, three)),
            new Vertex(new Vector3(1, -1, -1),  new Vector3(1, 0, 0),  new Vector2(one, two)),
            new Vertex(new Vector3(1, 1, -1),   new Vector3(1, 0, 0),  new Vector2(two, two)),
            new Vertex(new Vector3(1, -1, -1),  new Vector3(1, 0, 0),  new Vector2(one, two)),
            new Vertex(new Vector3(1, 1, 1),    new Vector3(1, 0, 0),  new Vector2(two, three)),
            new Vertex(new Vector3(1, -1, 1),   new Vector3(1, 0, 0),  new Vector2(one, three)),
            new Vertex(new Vector3(1, 1, 1),    new Vector3(0, 1, 0),  new Vector2(zero, two)),
            new Vertex(new Vector3(1, 1, -1),   new Vector3(0, 1, 0),  new Vector2(zero, one)),
            new Vertex(new Vector3(-1, 1, -1),  new Vector3(0, 1, 0),  new Vector2(one, one)),
            new Vertex(new Vector3(1, 1, 1),    new Vector3(0, 1, 0),  new Vector2(zero, two)),
            new Vertex(new Vector3(-1, 1, -1),  new Vector3(0, 1, 0),  new Vector2(one, one)),
            new Vertex(new Vector3(-1, 1, 1),   new Vector3(0, 1, 0),  new Vector2(one, two)),
            new Vertex(new Vector3(1, 1, 1),    new Vector3(0, 0, 1),  new Vector2(two, one)),
            new Vertex(new Vector3(-1, 1, 1),   new Vector3(0, 0, 1),  new Vector2(three, one)),
            new Vertex(new Vector3(1, -1, 1),   new Vector3(0, 0, 1),  new Vector2(two, two)),
        };
        private static readonly int[] _indexArray = Enumerable.Range(0, _vertexArray.Length).ToArray();

        protected override void OnActivated()
        {
            base.OnActivated();
            InitGraphicBuffer(_vertexArray, _indexArray);
        }
    }
}
