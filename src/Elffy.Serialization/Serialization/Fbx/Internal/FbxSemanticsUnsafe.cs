#nullable enable
using System;
using FbxTools;
using Elffy.Effective.Unsafes;
using Elffy.Effective;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Serialization.Fbx.Internal
{
    internal readonly struct FbxSemanticsUnsafe<TVertex> : IDisposable where TVertex : unmanaged, IVertex
    {
        private readonly FbxObject? _fbx;
        private readonly UnsafeRawArray<int> _indices;
        private readonly UnsafeRawArray<TVertex> _vertices;
        private readonly ValueTypeRentMemory<RawString> _textures;
        private readonly SkeletonDataList _skeletons;

        public ReadOnlySpan<int> Indices => _indices.AsSpan();

        public ReadOnlySpan<TVertex> Vertices => _vertices.AsSpan();

        public ReadOnlySpan<RawString> Textures => _textures.AsSpan();

        public ReadOnlySpan<SkeletonData> Skeletons => _skeletons.Span;

        internal FbxSemanticsUnsafe([MaybeNull] ref FbxObject fbx,
                                    ref UnsafeRawArray<int> indices,
                                    ref UnsafeRawArray<TVertex> vertices,
                                    ref ValueTypeRentMemory<RawString> texture,
                                    ref SkeletonDataList skeletons)
        {
            (_fbx, fbx) = (fbx, default);
            (_textures, texture) = (texture, default);
            (_indices, indices) = (indices, default);
            (_vertices, vertices) = (vertices, default);
            (_skeletons, skeletons) = (skeletons, default);
        }

        public void Dispose()
        {
            _fbx?.Dispose();
            _textures.Dispose();
            _indices.Dispose();
            _vertices.Dispose();
            _skeletons.Dispose();
        }

        public void OnFinalized()
        {
            // Don't touch _fbx

            _textures.Dispose();
            _indices.Dispose();
            _vertices.Dispose();
            _skeletons.Dispose();
        }
    }
}
