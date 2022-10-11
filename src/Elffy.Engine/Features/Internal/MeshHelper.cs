#nullable enable
using Elffy.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Features.Internal
{
    internal static class MeshHelper
    {
        public static unsafe bool TryGetMesh(Renderable renderable, [MaybeNullWhen(false)] out Mesh mesh)
        {
            ArgumentNullException.ThrowIfNull(renderable);

            if(renderable.TryGetScreen(out var screen) == false) { goto FAILURE; }
            if(Engine.CurrentContext != screen) { goto FAILURE; }
            if(renderable.IsLoaded == false) { goto FAILURE; }

            const uint IndexSize = sizeof(int);

            var vertexType = renderable.VertexType;
            Debug.Assert(vertexType != null);
            if(VertexTypeData.TryGetVertexTypeData(vertexType, out var vertexTypeData) == false) { goto FAILURE; }

            var vertexSize = (ulong)vertexTypeData.VertexSize;
            Debug.Assert(vertexType != null);

            var vbo = renderable.VBO;
            var ibo = renderable.IBO;

            var verticesCount = vbo.Length;
            var indicesCount = ibo.Length;
            var verticesByteSize = verticesCount * vertexSize;
            var indicesByteSize = indicesCount * IndexSize;

            var bufLen = verticesByteSize + indicesByteSize;
            var buf = UniquePtr.Malloc(checked((nuint)bufLen));
            var vDest = (void*)buf.Ptr;
            var iDest = buf.GetPtr<byte>() + verticesByteSize;
            try {
                try {
                    VBO.Bind(vbo);
                    var vSource = (void*)VBO.MapBufferReadOnly();
                    Buffer.MemoryCopy(vSource, vDest, bufLen, verticesByteSize);
                }
                finally {
                    VBO.UnmapBuffer();
                    VBO.Unbind();
                }
                try {
                    IBO.Bind(ibo);
                    var iSource = (void*)IBO.MapBufferReadOnly();
                    Buffer.MemoryCopy(iSource, iDest, bufLen, indicesByteSize);
                }
                finally {
                    IBO.UnmapBuffer();
                    IBO.Unbind();
                }
                mesh = Mesh.Create(vertexTypeData, IndexSize, vDest, verticesByteSize, iDest, indicesByteSize, ref buf, static buf => buf.Dispose());
                return true;
            }
            finally {
                buf.Dispose();
            }

        FAILURE:
            mesh = null;
            return false;
        }

        public static unsafe bool TryGetMeshRaw(
            Renderable renderable,
            void* vertices, ulong verticesByteSize,
            int* indices, ulong indicesByteSize,
            [MaybeNullWhen(false)] out Type vertexType,
            out ulong verticesByteSizeActual,
            out uint indicesByteSizeActual)
        {
            ArgumentNullException.ThrowIfNull(renderable);

            if(renderable.TryGetScreen(out var screen) == false) { goto FAILURE; }
            if(Engine.CurrentContext != screen) { goto FAILURE; }
            if(renderable.IsLoaded == false) { goto FAILURE; }
            vertexType = renderable.VertexType;
            Debug.Assert(vertexType != null);
            if(VertexTypeData.TryGetVertexTypeData(vertexType, out var vertexTypeData) == false) { goto FAILURE; }

            var vertexSize = (ulong)vertexTypeData.VertexSize;
            Debug.Assert(vertexType != null);

            var vbo = renderable.VBO;
            var ibo = renderable.IBO;

            var verticesCount = vbo.Length;
            var indicesCount = ibo.Length;
            verticesByteSizeActual = verticesCount * vertexSize;
            indicesByteSizeActual = indicesCount * sizeof(int);

            if(verticesByteSize < verticesByteSizeActual) { goto FAILURE; }
            if(indicesByteSize < indicesByteSizeActual) { goto FAILURE; }

            try {
                VBO.Bind(vbo);
                var vSource = (void*)VBO.MapBufferReadOnly();
                Buffer.MemoryCopy(vSource, vertices, verticesByteSize, verticesByteSizeActual);
            }
            finally {
                VBO.UnmapBuffer();
                VBO.Unbind();
            }
            try {
                IBO.Bind(ibo);
                var iSource = (void*)IBO.MapBufferReadOnly();
                Buffer.MemoryCopy(iSource, indices, indicesByteSize, indicesByteSizeActual);
            }
            finally {
                IBO.UnmapBuffer();
                IBO.Unbind();
            }
            return true;

        FAILURE:
            vertexType = null;
            verticesByteSizeActual = 0;
            indicesByteSizeActual = 0;
            return false;
        }

        public static unsafe (int VertexCount, int IndexCount) GetMesh<TVertex>(Renderable renderable, Span<TVertex> vertices, Span<int> indices) where TVertex : unmanaged, IVertex
        {
            fixed(TVertex* v = vertices)
            fixed(int* i = indices) {
                var (vCount, iCount) = GetMesh(renderable, v, (ulong)vertices.Length, i, (uint)indices.Length);
                return ((int)vCount, (int)iCount);
            }
        }

        public static unsafe (ulong VertexCount, uint IndexCount) GetMesh<TVertex>(Renderable renderable, TVertex* vertices, ulong vertexCount, int* indices, uint indexCount) where TVertex : unmanaged, IVertex
        {
            ArgumentNullException.ThrowIfNull(renderable);
            var screen = renderable.GetValidScreen();
            var currentContext = Engine.CurrentContext;
            if(currentContext != screen) {
                ContextMismatchException.Throw(currentContext, screen);
            }
            if(renderable.IsLoaded == false) {
                return (0, 0);
            }
            var vertexType = renderable.VertexType;
            Debug.Assert(vertexType != null);
            if(typeof(TVertex) != vertexType) {
                throw new InvalidOperationException($"Vertex type is invalid. (Vertex Type={vertexType?.FullName}, SpecifiedVertexType={typeof(TVertex).FullName})");
            }

            var vbo = renderable.VBO;
            var ibo = renderable.IBO;

            var vCount = vbo.Length;
            var iCount = ibo.Length;
            if(vertexCount < vCount) {
                throw new ArgumentOutOfRangeException($"{nameof(vertexCount)} is too short.");
            }
            if(indexCount < iCount) {
                throw new ArgumentOutOfRangeException($"{nameof(indexCount)} is too short.");
            }

            try {
                VBO.Bind(vbo);
                var vSource = (void*)VBO.MapBufferReadOnly();
                Buffer.MemoryCopy(vSource, vertices, vertexCount * (ulong)sizeof(TVertex), vCount * (ulong)sizeof(TVertex));
            }
            finally {
                VBO.UnmapBuffer();
                VBO.Unbind();
            }
            try {
                IBO.Bind(ibo);
                var iSource = (void*)IBO.MapBufferReadOnly();
                Buffer.MemoryCopy(iSource, indices, indexCount * sizeof(int), iCount * sizeof(int));
            }
            finally {
                IBO.UnmapBuffer();
                IBO.Unbind();
            }
            return (vCount, iCount);
        }
    }
}
