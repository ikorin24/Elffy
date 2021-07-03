#nullable enable
using System;
using FbxTools;
using Elffy.Effective.Unsafes;
using Elffy.Core;
using Elffy.Effective;

namespace Elffy.Serialization.Fbx
{
    internal readonly struct FbxSemantics : IDisposable
    {
        private readonly FbxObject? _fbx;
        private readonly UnsafeRawArray<int> _indices;
        private readonly UnsafeRawArray<Vertex> _vertices;
        private readonly ValueTypeRentMemory<RawString> _textures;

        public ReadOnlySpan<int> Indices => _indices.AsSpan();

        public ReadOnlySpan<Vertex> Vertices => _vertices.AsSpan();

        public ReadOnlySpan<RawString> Textures => _textures.Span;

        internal FbxSemantics(FbxObject fbx, UnsafeRawArray<int> indices, UnsafeRawArray<Vertex> vertices, ref ValueTypeRentMemory<RawString> texture)
        {
            _fbx = fbx;
            (_textures, texture) = (texture, default);
            _indices = indices;
            _vertices = vertices;
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
