#nullable enable
using System.Linq;
using OpenTK;
using Elffy.Core;
using System;

namespace Elffy.Shapes
{
    public class Cube : Renderable
    {
        private const float A = 0.5f;

        private const float zero = 0.0f;
        private const float one = 1 / 3.0f;
        private const float two = 2 / 3.0f;
        private const float three = 1.0f;

        private static readonly ReadOnlyMemory<Vertex> _vertexArray = new Vertex[36] {
            new Vertex(new Vector3(-A, -A, -A), new Vector3(-1, 0, 0), new Vector2(zero,    zero)),
            new Vertex(new Vector3(-A, -A, A),  new Vector3(-1, 0, 0), new Vector2(zero,    one)),
            new Vertex(new Vector3(-A, A, A),   new Vector3(-1, 0, 0), new Vector2(one,     one)),
            new Vertex(new Vector3(A, A, -A),   new Vector3(0, 0, -1), new Vector2(three,   zero)),
            new Vertex(new Vector3(-A, -A, -A), new Vector3(0, 0, -1), new Vector2(two,     one)),
            new Vertex(new Vector3(-A, A, -A),  new Vector3(0, 0, -1), new Vector2(three,   one)),
            new Vertex(new Vector3(A, -A, A),   new Vector3(0, -1, 0), new Vector2(two,     one)),
            new Vertex(new Vector3(-A, -A, -A), new Vector3(0, -1, 0), new Vector2(one,     two)),
            new Vertex(new Vector3(A, -A, -A),  new Vector3(0, -1, 0), new Vector2(two,     two)),
            new Vertex(new Vector3(A, A, -A),   new Vector3(0, 0, -1), new Vector2(three,   zero)),
            new Vertex(new Vector3(A, -A, -A),  new Vector3(0, 0, -1), new Vector2(two,     zero)),
            new Vertex(new Vector3(-A, -A, -A), new Vector3(0, 0, -1), new Vector2(two,     one)),
            new Vertex(new Vector3(-A, -A, -A), new Vector3(-1, 0, 0), new Vector2(zero,    zero)),
            new Vertex(new Vector3(-A, A, A),   new Vector3(-1, 0, 0), new Vector2(one,     one)),
            new Vertex(new Vector3(-A, A, -A),  new Vector3(-1, 0, 0), new Vector2(one,     zero)),
            new Vertex(new Vector3(A, -A, A),   new Vector3(0, -1, 0), new Vector2(two,     one)),
            new Vertex(new Vector3(-A, -A, A),  new Vector3(0, -1, 0), new Vector2(one,     one)),
            new Vertex(new Vector3(-A, -A, -A), new Vector3(0, -1, 0), new Vector2(one,     two)),
            new Vertex(new Vector3(-A, A, A),   new Vector3(0, 0, 1),  new Vector2(three,   two)),
            new Vertex(new Vector3(-A, -A, A),  new Vector3(0, 0, 1),  new Vector2(three,   one)),
            new Vertex(new Vector3(A, -A, A),   new Vector3(0, 0, 1),  new Vector2(two,     one)),
            new Vertex(new Vector3(A, A, A),    new Vector3(1, 0, 0),  new Vector2(two,     zero)),
            new Vertex(new Vector3(A, -A, -A),  new Vector3(1, 0, 0),  new Vector2(one,     one)),
            new Vertex(new Vector3(A, A, -A),   new Vector3(1, 0, 0),  new Vector2(two,     one)),
            new Vertex(new Vector3(A, -A, -A),  new Vector3(1, 0, 0),  new Vector2(one,     one)),
            new Vertex(new Vector3(A, A, A),    new Vector3(1, 0, 0),  new Vector2(two,     zero)),
            new Vertex(new Vector3(A, -A, A),   new Vector3(1, 0, 0),  new Vector2(one,     zero)),
            new Vertex(new Vector3(A, A, A),    new Vector3(0, 1, 0),  new Vector2(zero,    one)),
            new Vertex(new Vector3(A, A, -A),   new Vector3(0, 1, 0),  new Vector2(zero,    two)),
            new Vertex(new Vector3(-A, A, -A),  new Vector3(0, 1, 0),  new Vector2(one,     two)),
            new Vertex(new Vector3(A, A, A),    new Vector3(0, 1, 0),  new Vector2(zero,    one)),
            new Vertex(new Vector3(-A, A, -A),  new Vector3(0, 1, 0),  new Vector2(one,     two)),
            new Vertex(new Vector3(-A, A, A),   new Vector3(0, 1, 0),  new Vector2(one,     one)),
            new Vertex(new Vector3(A, A, A),    new Vector3(0, 0, 1),  new Vector2(two,     two)),
            new Vertex(new Vector3(-A, A, A),   new Vector3(0, 0, 1),  new Vector2(three,   two)),
            new Vertex(new Vector3(A, -A, A),   new Vector3(0, 0, 1),  new Vector2(two,     one)),
        };
        private static readonly ReadOnlyMemory<int> _indexArray = Enumerable.Range(0, _vertexArray.Length).ToArray();

        public Cube()
        {
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            LoadGraphicBuffer(_vertexArray.Span, _indexArray.Span);
        }
    }
}
