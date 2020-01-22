#nullable enable
using Elffy.Core;
using Elffy.Exceptions;
using OpenTK;
using System;

namespace Elffy
{
    //public class Sky : Renderable
    //{
    //    private readonly Vertex[] _vertexArray;
    //    private readonly int[] _indexArray;

    //    public Sky(float r)
    //    {
    //        ArgumentChecker.ThrowOutOfRangeIf(r <= 0, nameof(r), r, $"{nameof(r)} is out of range");
    //        const int a = 16;
    //        const int b = 16;
    //        GenerateVertex(r, a, b, out _vertexArray, out _indexArray);
    //        Activated += OnActivated;
    //    }

    //    private void GenerateVertex(float r, int a, int b, out Vertex[] vertexArray, out int[] indexArray)
    //    {
    //        vertexArray = new Vertex[(a + 1) * (b + 1)];
    //        for(int j = 0; j < a + 1; j++) {
    //            var phi = MathHelper.PiOver2 - MathHelper.Pi / a * j;
    //            for(int i = 0; i < b + 1; i++) {
    //                var theta = MathHelper.TwoPi / b * i;
    //                var cosPhi = Math.Cos(phi);
    //                var cosTheta = Math.Cos(theta);
    //                var sinPhi = Math.Sin(phi);
    //                var sinTheta = Math.Sin(theta);
    //                var pos = new Vector3((float)(r * cosPhi * cosTheta), (float)(r * sinPhi), (float)(r * cosPhi * sinTheta));
    //                var normal = -pos.Normalized();
    //                var texCoord = new Vector2((float)i / b, 1 - (float)j / a);
    //                vertexArray[(b + 1) * j + i] = new Vertex(pos, normal, texCoord);
    //            }
    //        }
    //        indexArray = new int[a * b * 6];
    //        for(int j = 0; j < a; j++) {
    //            for(int i = 0; i < b; i++) {
    //                var l = (b * j + i) * 6;
    //                indexArray[l] = (b + 1) * j + i;
    //                indexArray[l + 1] = (b + 1) * (j + 1) + i;
    //                indexArray[l + 2] = (b + 1) * (j + 1) + (i + 1) % (b + 1);
    //                indexArray[l + 3] = (b + 1) * j + i;
    //                indexArray[l + 4] = (b + 1) * (j + 1) + (i + 1) % (b + 1);
    //                indexArray[l + 5] = (b + 1) * j + (i + 1) % (b + 1);
    //            }
    //        }
    //    }

    //    private void OnActivated(FrameObject frameObject)
    //    {
    //        InitGraphicBuffer(_vertexArray, _indexArray);
    //    }
    //}
}
