#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy.Shapes
{
    /// <summary>Square plain 3D object</summary>
    public class Plain : Renderable
    {
        /// <summary>Create new <see cref="Plain"/></summary>
        public Plain()
        {
        }

        [SkipLocalsInit]
        protected override UniTask<AsyncUnit> OnActivating(CancellationToken cancellationToken)
        {
            // [indices]
            //
            //        0 ----- 3
            //        |  \    |
            //   y    |    \  |
            //   ^    1 ----- 2
            //   |
            //   + ---> x
            //  /
            // z

            // [uv]
            // OpenGL coordinate of uv is left-bottom based,
            // but many popular format of images (e.g. png) are left-top based.
            // So, I use left-top as uv coordinate.
            //
            //         0 ----- 1  u
            //
            //    0    0 ----- 3
            //    |    |       |
            //    |    |       |
            //    1    1 ----- 2
            //    v

            const float a = 0.5f;
            ReadOnlySpan<Vertex> vertice = stackalloc Vertex[4]
            {
                new(new(-a,  a, 0), new(0, 0, 1), new(0, 0)),
                new(new(-a, -a, 0), new(0, 0, 1), new(0, 1)),
                new(new( a, -a, 0), new(0, 0, 1), new(1, 1)),
                new(new( a,  a, 0), new(0, 0, 1), new(1, 0)),
            };
            ReadOnlySpan<int> indices = stackalloc int[6]
            {
                0, 1, 2, 0, 2, 3,
            };
            LoadMesh(vertice, indices);
            return new UniTask<AsyncUnit>(AsyncUnit.Default);
        }
    }
}
