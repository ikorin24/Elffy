#nullable enable
using Cysharp.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Elffy.Shapes
{
    /// <summary>Square plain 3D object</summary>
    public class Plain : Renderable
    {
        /// <summary>Create new <see cref="Plain"/></summary>
        public Plain()
        {
            Activating.Subscribe((f, ct) => SafeCast.As<Plain>(f).OnActivating());
        }

        [SkipLocalsInit]
        private unsafe UniTask OnActivating()
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
            const int VertexCount = 4;
            const int IndexCount = 6;
            Vertex* vertice = stackalloc Vertex[VertexCount]
            {
                new(new(-a,  a, 0f), new(0f, 0f, 1f), new(0f, 0f)),
                new(new(-a, -a, 0f), new(0f, 0f, 1f), new(0f, 1f)),
                new(new( a, -a, 0f), new(0f, 0f, 1f), new(1f, 1f)),
                new(new( a,  a, 0f), new(0f, 0f, 1f), new(1f, 0f)),
            };
            int* indices = stackalloc int[IndexCount]
            {
                0, 1, 2, 0, 2, 3,
            };
            LoadMesh(vertice, VertexCount, indices, IndexCount);
            return new UniTask<AsyncUnit>(AsyncUnit.Default);
        }
    }
}
