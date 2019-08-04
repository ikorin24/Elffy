using Elffy.Core;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    public class Sky : Renderable
    {
        private readonly Vertex[] _vertexArray;
        private readonly int[] _indexArray;

        public Sky(float r)
        {
            if(r <= 0) { throw new ArgumentException(); }
            const int a = 16;
            const int b = 16;
            _vertexArray = new Vertex[(a + 1) * b];
            for(int j = 0; j < a + 1; j++) {
                var phi = MathHelper.PiOver2 - MathHelper.Pi / a * j;
                for(int i = 0; i < b; i++) {
                    var theta = MathHelper.Pi * 2 / b * i;
                    var pos = new Vector3((float)(r * Math.Cos(phi) * Math.Cos(theta)), (float)(r * Math.Sin(phi)), (float)(r * Math.Cos(phi) * Math.Sin(theta)));
                    var normal = -pos.Normalized();
                    var texCoord = new Vector2();       // TODO: テクスチャ座標
                    _vertexArray[b * j + i] = new Vertex(pos, normal, texCoord);
                }
            }
            _indexArray = new int[a * b * 6];
            for(int k = 0; k < a * b; k++) {
                var l = k * 6;
                _indexArray[l] = k;
                _indexArray[l + 1] = (k / b * b + b) + (k + 1) % b;
                _indexArray[l + 2] = (k / b * b) + (k + 1) % b;
                _indexArray[l + 3] = k;
                _indexArray[l + 4] = k + b;
                _indexArray[l + 5] = (k / b * b + b) + (k + 1) % b;
            }
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            InitGraphicBuffer(_vertexArray, _indexArray);
        }
    }
}
