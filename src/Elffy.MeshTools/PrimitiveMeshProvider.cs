#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    public static unsafe class PrimitiveMeshProvider<TVertex> where TVertex : unmanaged
    {
        public static void LoadPlain(MeshLoadAction<TVertex> action)
        {
            LoadPlain(action, static (action, vertices, indices) => action(vertices, indices));
        }

        public static void LoadPlain<TState>(TState state, MeshLoadAction<TState, TVertex> action)
        {
            ArgumentNullException.ThrowIfNull(action);

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
            int* indices = stackalloc int[IndexCount] { 0, 1, 2, 0, 2, 3, };

            if(typeof(TVertex) == typeof(Vertex)) {
                ForVertex(state, action, indices);
            }
            else if(typeof(TVertex) == typeof(VertexSlim)) {
                ForVertexSlim(state, action, indices);
            }
            else {
                ForOthers(state, action, indices);
            }
            return;

            static void ForVertex(TState state, MeshLoadAction<TState, TVertex> action, int* indices)
            {
                Debug.Assert(typeof(TVertex) == typeof(Vertex));
                Vertex* vertices = stackalloc Vertex[VertexCount]
                {
                    new(new(-a, a, 0f), new(0f, 0f, 1f), new(0f, 0f)),
                    new(new(-a, -a, 0f), new(0f, 0f, 1f), new(0f, 1f)),
                    new(new(a, -a, 0f), new(0f, 0f, 1f), new(1f, 1f)),
                    new(new(a, a, 0f), new(0f, 0f, 1f), new(1f, 0f)),
                };
                action.Invoke(state, new ReadOnlySpan<TVertex>(vertices, VertexCount), new ReadOnlySpan<int>(indices, IndexCount));
            }

            static void ForVertexSlim(TState state, MeshLoadAction<TState, TVertex> action, int* indices)
            {
                Debug.Assert(typeof(TVertex) == typeof(VertexSlim));
                VertexSlim* vertices = stackalloc VertexSlim[VertexCount]
                {
                    new(new(-a, a, 0f), new(0f, 0f)),
                    new(new(-a, -a, 0f), new(0f, 1f)),
                    new(new(a, -a, 0f), new(1f, 1f)),
                    new(new(a, a, 0f), new(1f, 0f)),
                };
                action.Invoke(state, new ReadOnlySpan<TVertex>(vertices, VertexCount), new ReadOnlySpan<int>(indices, IndexCount));
            }

            static void ForOthers(TState state, MeshLoadAction<TState, TVertex> action, int* indices)
            {
                if(VertexMarshalHelper.TryGetVertexTypeData(typeof(TVertex), out var typeData) == false) {
                    ThrowInvalidVertexType();
                }
                TVertex* vertices = stackalloc TVertex[VertexCount];
                var verticesSpan = new Span<TVertex>(vertices, VertexCount);
                verticesSpan.Clear();

                if(typeData.TryGetField(VertexSpecialField.Position, out var pos) == false) {
                    ThrowInvalidVertexType();
                }
                else {
                    var posOffset = pos.ByteOffset;
                    *(Vector3*)(((byte*)(vertices + 0)) + posOffset) = new(-a, a, 0f);
                    *(Vector3*)(((byte*)(vertices + 1)) + posOffset) = new(-a, -a, 0f);
                    *(Vector3*)(((byte*)(vertices + 2)) + posOffset) = new(a, -a, 0f);
                    *(Vector3*)(((byte*)(vertices + 3)) + posOffset) = new(a, a, 0f);
                }

                if(typeData.TryGetField(VertexSpecialField.Normal, out var normal)) {
                    var normalOffset = normal.ByteOffset;
                    *(Vector3*)(((byte*)(vertices + 0)) + normalOffset) = new(0f, 0f, 1f);
                    *(Vector3*)(((byte*)(vertices + 1)) + normalOffset) = new(0f, 0f, 1f);
                    *(Vector3*)(((byte*)(vertices + 2)) + normalOffset) = new(0f, 0f, 1f);
                    *(Vector3*)(((byte*)(vertices + 3)) + normalOffset) = new(0f, 0f, 1f);
                }
                if(typeData.TryGetField(VertexSpecialField.UV, out var uv)) {
                    var uvOffset = uv.ByteOffset;
                    *(Vector2*)(((byte*)(vertices + 0)) + uvOffset) = new(0, 0);
                    *(Vector2*)(((byte*)(vertices + 1)) + uvOffset) = new(0, 1);
                    *(Vector2*)(((byte*)(vertices + 2)) + uvOffset) = new(1, 1);
                    *(Vector2*)(((byte*)(vertices + 3)) + uvOffset) = new(1, 0);
                }
                action.Invoke(state, verticesSpan, new ReadOnlySpan<int>(indices, IndexCount));
            }
        }

        [DoesNotReturn]
        private static void ThrowInvalidVertexType() => throw new InvalidOperationException($"The type is not supported vertex type. (Type = {typeof(TVertex).FullName})");
    }

    public delegate void MeshLoadAction<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged;
    public delegate void MeshLoadAction<TState, TVertex>(TState state, ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged;
}
