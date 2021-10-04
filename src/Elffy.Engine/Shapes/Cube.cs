#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Elffy.Shapes
{
    public class Cube : Renderable
    {
        public Cube()
        {
        }

        [SkipLocalsInit]
        protected override UniTask<AsyncUnit> OnActivating(CancellationToken cancellationToken)
        {
            // [indices]
            //             0 ------- 3
            //             |         |
            //             |   up    |
            //             |         |
            //             1 ------- 2
            // 4 ------- 7 8 -------11 12-------15 16-------19
            // |         | |         | |         | |         |
            // |  left   | |  front  | |  right  | |  back   |
            // |         | |         | |         | |         |
            // 5 ------- 6 9 -------10 13-------14 17-------18
            //             20-------23
            //             |         |
            //             |  down   |
            //             |         |
            //             21-------22

            // [uv]
            // OpenGL coordinate of uv is left-bottom based,
            // but many popular format of images (e.g. png) are left-top based.
            // So, I use left-top as uv coordinate.
            //
            //       0 ------ 1/4 ----- 1/2 ----- 3/4 ------ 1
            //
            //   0   o --> u   + ------- +
            //   |   |         |         |
            //   |   v         |   up    |
            //   |             |         |
            //  1/3  + ------- + ------- + ------- + ------- +
            //   |   |         |         |         |         |
            //   |   |  left   |  front  |  right  |  back   |
            //   |   |         |         |         |         |
            //  2/3  + ------- + ------- + ------- + ------- +
            //   |             |         |
            //   |             |  down   |
            //   |             |         |
            //   1             + ------- +

            // [shape]
            // Coordinate origin is center of the box.
            //
            //     + ------- +
            //    /   up    /|
            //   + ------- + |
            //   |         | ← right
            //   |  front  | +
            //   |         |/
            //   + ------- +

            const float a = 0.5f;
            const float b0 = 0f;
            const float b1 = 1f/4f;
            const float b2 = 2f/4f;
            const float b3 = 3f/4f;
            const float b4 = 1f;
            const float c0 = 0f;
            const float c1 = 1f/3f;
            const float c2 = 2f/3f;
            const float c3 = 1f;
            ReadOnlySpan<Vertex> vertices = stackalloc Vertex[24]
            {
                new(new(-a,  a, -a), new( 0,  1,  0), new(b1, c0)), new(new(-a,  a,  a), new( 0,  1,  0), new(b1, c1)), new(new( a,  a,  a), new( 0,  1,  0), new(b2, c1)), new(new( a,  a, -a), new( 0,  1,  0), new(b2, c0)),
                new(new(-a,  a, -a), new(-1,  0,  0), new(b0, c1)), new(new(-a, -a, -a), new(-1,  0,  0), new(b0, c2)), new(new(-a, -a,  a), new(-1,  0,  0), new(b1, c2)), new(new(-a,  a,  a), new(-1,  0,  0), new(b1, c1)),
                new(new(-a,  a,  a), new( 0,  0,  1), new(b1, c1)), new(new(-a, -a,  a), new( 0,  0,  1), new(b1, c2)), new(new( a, -a,  a), new( 0,  0,  1), new(b2, c2)), new(new( a,  a,  a), new( 0,  0,  1), new(b2, c1)),
                new(new( a,  a,  a), new( 1,  0,  0), new(b2, c1)), new(new( a, -a,  a), new( 1,  0,  0), new(b2, c2)), new(new( a, -a, -a), new( 1,  0,  0), new(b3, c2)), new(new( a,  a, -a), new( 1,  0,  0), new(b3, c1)),
                new(new( a,  a, -a), new( 0,  0, -1), new(b3, c1)), new(new( a, -a, -a), new( 0,  0, -1), new(b3, c2)), new(new(-a, -a, -a), new( 0,  0, -1), new(b4, c2)), new(new(-a,  a, -a), new( 0,  0, -1), new(b4, c1)),
                new(new(-a, -a,  a), new( 0, -1,  0), new(b1, c2)), new(new(-a, -a, -a), new( 0, -1,  0), new(b1, c3)), new(new( a, -a, -a), new( 0, -1,  0), new(b2, c3)), new(new( a, -a,  a), new( 0, -1,  0), new(b2, c2)),
            };
            ReadOnlySpan<int> indices = stackalloc int[36]
            {
                0, 1, 2, 0, 2, 3,         // up
                4, 5, 6, 4, 6, 7,         // left
                8, 9, 10, 8, 10, 11,      // front
                12, 13, 14, 12, 14, 15,   // right
                16, 17, 18, 16, 18, 19,   // back
                20, 21, 22, 20, 22, 23,   // down
            };
            LoadMesh(vertices, indices);
            return new UniTask<AsyncUnit>(AsyncUnit.Default);
        }
    }
}
