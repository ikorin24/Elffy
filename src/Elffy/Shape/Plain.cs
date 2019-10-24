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

namespace Elffy.Shape
{
    /// <summary>正方形の平面の3Dオブジェクトクラス</summary>
    public class Plain : Renderable
    {
        private static readonly Vertex[] _vertexArray = new Vertex[4]
        {
            new Vertex(new Vector3(-1, 1, 0),  new Vector3(0, 0, 1), new Vector2(0, 1)),
            new Vertex(new Vector3(-1, -1, 0), new Vector3(0, 0, 1), new Vector2(0, 0)),
            new Vertex(new Vector3(1, -1, 0),  new Vector3(0, 0, 1), new Vector2(1, 0)),
            new Vertex(new Vector3(1, 1, 0),   new Vector3(0, 0, 1), new Vector2(1, 1)),
        };
        private static readonly Vertex[] _inverseTexCoordYVertexArray = new Vertex[4]
        {
            new Vertex(new Vector3(-1, 1, 0),  new Vector3(0, 0, 1), new Vector2(0, 0)),
            new Vertex(new Vector3(-1, -1, 0), new Vector3(0, 0, 1), new Vector2(0, 1)),
            new Vertex(new Vector3(1, -1, 0),  new Vector3(0, 0, 1), new Vector2(1, 1)),
            new Vertex(new Vector3(1, 1, 0),   new Vector3(0, 0, 1), new Vector2(1, 0)),
        };

        private static readonly int[] _indexArray = new int[6] { 0, 1, 2, 2, 3, 0 };

        private bool _isTexCoordYInversed;

        /// <summary>平面の3Dオブジェクトを生成します</summary>
        public Plain()
        {
            Activated += OnActivated;
        }

        /// <summary>平面の3Dオブジェクトを生成します</summary>
        /// <param name="isTexCoordYInversed">テクスチャのY軸方向を反転させる場合 true</param>
        public Plain(bool isTexCoordYInversed)
        {
            _isTexCoordYInversed = isTexCoordYInversed;
        }

        private void OnActivated()
        {
            var vertexArray = _isTexCoordYInversed ? _inverseTexCoordYVertexArray : _vertexArray;
            InitGraphicBuffer(vertexArray, _indexArray);
        }
    }
}
