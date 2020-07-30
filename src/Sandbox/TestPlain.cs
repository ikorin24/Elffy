#nullable enable
using Elffy;
using Elffy.Core;
using Elffy.Shapes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sandbox
{
    public sealed class TestPlain : Renderable
    {
        private const float A = 0.5f;

        private static readonly ReadOnlyMemory<RigVertex> _vertexArray = new RigVertex[4]
        {
            new RigVertex(new Vector3(-A, A, 0),  new Vector3(0, 0, 1), new Vector2(0, 0), new Vector4i(0, 0, 0, 0), new Vector4()),
            new RigVertex(new Vector3(-A, -A, 0), new Vector3(0, 0, 1), new Vector2(0, 1), new Vector4i(0, 1, 0, 0), new Vector4()),
            new RigVertex(new Vector3(A, -A, 0),  new Vector3(0, 0, 1), new Vector2(1, 1), new Vector4i(0, 2, 0, 0), new Vector4()),
            new RigVertex(new Vector3(A, A, 0),   new Vector3(0, 0, 1), new Vector2(1, 0), new Vector4i(0, 3, 0, 0), new Vector4()),
        };

        private static readonly ReadOnlyMemory<int> _indexArray = new int[6] { 0, 1, 2, 2, 3, 0 };

        public TestPlain()
        {
        }

        protected override void OnAlive()
        {
            base.OnAlive();
            LoadGraphicBuffer(_vertexArray.Span, _indexArray.Span);
        }
    }
}
