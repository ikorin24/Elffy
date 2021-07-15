#nullable enable
using System;
using FbxTools;
using Elffy.Effective.Unsafes;
using Elffy.Effective;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Serialization.Fbx
{
    internal readonly struct FbxSemantics<TVertex> : IDisposable where TVertex : unmanaged
    {
        private readonly FbxObject? _fbx;
        private readonly UnsafeRawArray<int> _indices;
        private readonly UnsafeRawArray<TVertex> _vertices;
        private readonly ValueTypeRentMemory<RawString> _textures;

        public ReadOnlySpan<int> Indices => _indices.AsSpan();

        public ReadOnlySpan<TVertex> Vertices => _vertices.AsSpan();

        public ReadOnlySpan<RawString> Textures => _textures.Span;

        internal FbxSemantics([MaybeNull] ref FbxObject fbx, ref UnsafeRawArray<int> indices, ref UnsafeRawArray<TVertex> vertices, ref ValueTypeRentMemory<RawString> texture)
        {
            (_fbx, fbx) = (fbx, default);
            (_textures, texture) = (texture, default);
            (_indices, indices) = (indices, default);
            (_vertices, vertices) = (vertices, default);
        }

        public void Dispose()
        {
            _fbx?.Dispose();
            _textures.Dispose();
            _indices.Dispose();
            _vertices.Dispose();
        }
    }
}
