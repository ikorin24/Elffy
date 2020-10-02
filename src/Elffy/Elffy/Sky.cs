#nullable enable
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.Mathematics;
using OpenTK;
using System;

namespace Elffy
{
    public class Sky : Renderable
    {
        public Sky()
        {
        }

        private void GenerateVertex(float r, int a, int b, out ValueTypeRentMemory<Vertex> vertices, out ValueTypeRentMemory<int> indices)
        {
            vertices = new ValueTypeRentMemory<Vertex>((a + 1) * (b + 1));
            indices = new ValueTypeRentMemory<int>(a * b * 6);
            var verticesSpan = vertices.Span;
            var indicesSpan = indices.Span;

            for(int j = 0; j < a + 1; j++) {
                var phi = MathTool.PiOver2 - MathTool.Pi / a * j;
                for(int i = 0; i < b + 1; i++) {
                    var theta = MathTool.TwoPi / b * i;
                    var cosPhi = MathF.Cos(phi);
                    var cosTheta = MathF.Cos(theta);
                    var sinPhi = MathF.Sin(phi);
                    var sinTheta = MathF.Sin(theta);
                    var pos = new Vector3((float)(r * cosPhi * cosTheta), (float)(r * sinPhi), (float)(r * cosPhi * sinTheta));
                    var normal = -pos.Normalized();
                    var texCoord = new Vector2((float)i / b, 1 - (float)j / a);
                    verticesSpan[(b + 1) * j + i] = new Vertex(pos, normal, texCoord);
                }
            }
            for(int j = 0; j < a; j++) {
                for(int i = 0; i < b; i++) {
                    var l = (b * j + i) * 6;
                    indicesSpan[l] = (b + 1) * j + i;
                    indicesSpan[l + 1] = (b + 1) * (j + 1) + i;
                    indicesSpan[l + 2] = (b + 1) * (j + 1) + (i + 1) % (b + 1);
                    indicesSpan[l + 3] = (b + 1) * j + i;
                    indicesSpan[l + 4] = (b + 1) * (j + 1) + (i + 1) % (b + 1);
                    indicesSpan[l + 5] = (b + 1) * j + (i + 1) % (b + 1);
                }
            }
        }

        protected override void OnAlive()
        {
            base.OnAlive();

            ValueTypeRentMemory<Vertex> vertices = default;
            ValueTypeRentMemory<int> indices = default;
            try {
                GenerateVertex(1, 16, 16, out vertices, out indices);
                LoadGraphicBuffer(vertices.Span, indices.Span);
            }
            finally {
                vertices.Dispose();
                indices.Dispose();
            }
        }
    }
}
