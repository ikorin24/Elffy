#nullable enable
using Elffy.Core;
using System;

namespace Elffy.Shape
{
    /// <summary>正方形の平面の3Dオブジェクトクラス</summary>
    public class Plain : Renderable
    {
        private const float A = 0.5f;

        private static readonly ReadOnlyMemory<Vertex> _vertexArray = new Vertex[4]
        {
            new Vertex(new Vector3(-A, A, 0),  new Vector3(0, 0, 1), new Vector2(0, 0)),
            new Vertex(new Vector3(-A, -A, 0), new Vector3(0, 0, 1), new Vector2(0, 1)),
            new Vertex(new Vector3(A, -A, 0),  new Vector3(0, 0, 1), new Vector2(1, 1)),
            new Vertex(new Vector3(A, A, 0),   new Vector3(0, 0, 1), new Vector2(1, 0)),
        };

        private static readonly ReadOnlyMemory<int> _indexArray = new int[6] { 0, 1, 2, 2, 3, 0 };

        /// <summary>平面の3Dオブジェクトを生成します</summary>
        public Plain()
        {
        }

        protected override void OnAlive()
        {
            base.OnAlive();
            LoadGraphicBuffer(_vertexArray.Span, _indexArray.Span);
        }
    }
}
