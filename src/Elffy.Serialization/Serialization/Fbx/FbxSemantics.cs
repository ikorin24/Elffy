#nullable enable
using System;
using FbxTools;
using Elffy.Serialization.Fbx.Internal;

namespace Elffy.Serialization.Fbx
{
    public sealed class FbxSemantics<TVertex> : IDisposable where TVertex : unmanaged, IVertex
    {
        private FbxSemanticsUnsafe<TVertex> _core;

        public ReadOnlySpan<int> Indices => _core.Indices;

        public ReadOnlySpan<TVertex> Vertices => _core.Vertices;

        public ReadOnlySpan<RawString> Textures => _core.Textures;

        public ReadOnlySpan<SkeletonData> Skeletons => _core.Skeletons;

        internal FbxSemantics(in FbxSemanticsUnsafe<TVertex> core)
        {
            _core = core;
        }

        ~FbxSemantics() => Dispose(false);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(disposing) {
                _core.Dispose();
            }
            else {
                _core.OnFinalized();
            }
        }
    }
}
