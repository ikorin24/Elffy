#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective.Unsafes;
using Elffy.Mathematics;
using System;

namespace Elffy.Shapes
{
    public class SkySphere : Renderable
    {
        public SkySphere()
        {
            Activating.Subscribe((f, ct) => SafeCast.As<SkySphere>(f).OnActivating());
        }

        private static void GenerateMesh(out UnsafeRawArray<Vertex> vertices, out UnsafeRawArray<int> indices)
        {
            const float r = 1;
            const int a = 16;
            const int b = 16;

            vertices = new UnsafeRawArray<Vertex>((a + 1) * (b + 1), false);
            indices = new UnsafeRawArray<int>(a * b * 6, false);

            // 'UnsafeRawArray' does not check boundary of index accessing. Be careful!

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
                    var uv = new Vector2((float)i / b, 1 - (float)j / a);
                    vertices[(b + 1) * j + i] = new Vertex(pos, normal, uv);
                }
            }
            for(int j = 0; j < a; j++) {
                for(int i = 0; i < b; i++) {
                    var l = (b * j + i) * 6;
                    indices[l] = (b + 1) * j + i;
                    indices[l + 1] = (b + 1) * (j + 1) + i;
                    indices[l + 2] = (b + 1) * (j + 1) + (i + 1) % (b + 1);
                    indices[l + 3] = (b + 1) * j + i;
                    indices[l + 4] = (b + 1) * (j + 1) + (i + 1) % (b + 1);
                    indices[l + 5] = (b + 1) * j + (i + 1) % (b + 1);
                }
            }
        }

        private UniTask OnActivating()
        {
            GenerateMesh(out var vertices, out var indices);
            using(vertices)
            using(indices) {
                LoadMesh<Vertex>(vertices.AsSpan(), indices.AsSpan());
            }
            return new UniTask<AsyncUnit>(AsyncUnit.Default);
        }
    }
}
