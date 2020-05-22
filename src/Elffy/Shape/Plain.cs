#nullable enable
using Elffy.Core;
using OpenTK;
using System;

namespace Elffy.Shape
{
    /// <summary>正方形の平面の3Dオブジェクトクラス</summary>
    public class Plain : Renderable
    {
        private static readonly ReadOnlyMemory<Vertex> _vertexArray = new Vertex[4]
        {
            new Vertex(new Vector3(-1, 1, 0),  new Vector3(0, 0, 1), new Vector2(0, 1)),
            new Vertex(new Vector3(-1, -1, 0), new Vector3(0, 0, 1), new Vector2(0, 0)),
            new Vertex(new Vector3(1, -1, 0),  new Vector3(0, 0, 1), new Vector2(1, 0)),
            new Vertex(new Vector3(1, 1, 0),   new Vector3(0, 0, 1), new Vector2(1, 1)),
        };
        private static readonly ReadOnlyMemory<Vertex> _inverseTexCoordYVertexArray = new Vertex[4]
        {
            new Vertex(new Vector3(-1, 1, 0),  new Vector3(0, 0, 1), new Vector2(0, 0)),
            new Vertex(new Vector3(-1, -1, 0), new Vector3(0, 0, 1), new Vector2(0, 1)),
            new Vertex(new Vector3(1, -1, 0),  new Vector3(0, 0, 1), new Vector2(1, 1)),
            new Vertex(new Vector3(1, 1, 0),   new Vector3(0, 0, 1), new Vector2(1, 0)),
        };

        private static readonly ReadOnlyMemory<int> _indexArray = new int[6] { 0, 1, 2, 2, 3, 0 };

        private bool _isTexCoordYInversed;

        /// <summary>平面の3Dオブジェクトを生成します</summary>
        public Plain() : this(false) { }

        /// <summary>平面の3Dオブジェクトを生成します</summary>
        /// <param name="isTexCoordYInversed">テクスチャのY軸方向を反転させる場合 true</param>
        public Plain(bool isTexCoordYInversed)
        {
            _isTexCoordYInversed = isTexCoordYInversed;
        }

        protected override void OnAlive()
        {
            base.OnAlive();
            var vertexArray = _isTexCoordYInversed ? _inverseTexCoordYVertexArray : _vertexArray;
            LoadGraphicBuffer(vertexArray.Span, _indexArray.Span);
        }
    }
}
