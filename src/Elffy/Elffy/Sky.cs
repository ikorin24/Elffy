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
        private bool _lightStateBuffer;

        public Sky(float r)
        {
            if(r <= 0) { throw new ArgumentException(); }
            const int a = 16;
            const int b = 16;
            _vertexArray = new Vertex[(a + 1) * (b + 1)];
            for(int j = 0; j < a + 1; j++) {
                var phi = MathHelper.PiOver2 - MathHelper.Pi / a * j;
                for(int i = 0; i < b + 1; i++) {
                    var theta = MathHelper.TwoPi / b * i;
                    var cosPhi = Math.Cos(phi);
                    var cosTheta = Math.Cos(theta);
                    var sinPhi = Math.Sin(phi);
                    var sinTheta = Math.Sin(theta);
                    var pos = new Vector3((float)(r * cosPhi * cosTheta), (float)(r * sinPhi), (float)(r * cosPhi * sinTheta));
                    var normal = -pos.Normalized();
                    var texCoord = new Vector2((float)i / b, 1 - (float)j / a);
                    _vertexArray[(b + 1) * j + i] = new Vertex(pos, normal, texCoord);
                }
            }
            _indexArray = new int[a * b * 6];
            for(int j = 0; j < a; j++) {
                for(int i = 0; i < b; i++) {
                    var l = (b  * j + i) * 6;
                    _indexArray[l]     = (b + 1) * j + i;
                    _indexArray[l + 1] = (b + 1) * (j + 1) + i;
                    _indexArray[l + 2] = (b + 1) * (j + 1) + (i + 1) % (b + 1);
                    _indexArray[l + 3] = (b + 1) * j + i;
                    _indexArray[l + 4] = (b + 1) * (j + 1) + (i + 1) % (b + 1);
                    _indexArray[l + 5] = (b + 1) * j + (i + 1) % (b + 1);
                }
            }
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            InitGraphicBuffer(_vertexArray, _indexArray);
        }

        protected override void OnRendering()
        {
            _lightStateBuffer = Light.IsEnabled;
            Light.IsEnabled = false;
        }

        protected override void OnRendered()
        {
            Light.IsEnabled = _lightStateBuffer;
        }
    }
}
