#nullable enable
using Elffy.Core;
using Elffy.Effective;
using Elffy.OpenGL;
using System;
using System.Diagnostics;

namespace Elffy.Components
{
    internal sealed class PmxModelParts : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<PmxModelParts> _core = new(true);

        private bool _disposed;
        private ValueTypeRentMemory<int> _vertexCountArray;
        private ValueTypeRentMemory<int> _textureIndexArray;
        private ValueTypeRentMemory<TextureObject> _textures;

        public ComponentOwner? Owner => _core.Owner;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        public ReadOnlySpan<int> VertexCountArray => _vertexCountArray.Span;

        public ReadOnlySpan<int> TextureIndexArray => _textureIndexArray.Span;

        public ReadOnlySpan<TextureObject> Textures => _textures.Span;

        public PmxModelParts(ref ValueTypeRentMemory<int> vertexCountArray,
                             ref ValueTypeRentMemory<int> textureIndexArray,
                             ref ValueTypeRentMemory<TextureObject> textures)
        {
            Debug.Assert(vertexCountArray.Length == textureIndexArray.Length);

            _vertexCountArray = vertexCountArray;
            _textureIndexArray = textureIndexArray;
            _textures = textures;
            vertexCountArray = default;
            textureIndexArray = default;
            textures = default;
        }

        public void OnAttached(ComponentOwner owner)
        {
            _core.OnAttached(owner);
        }

        public void OnDetached(ComponentOwner owner)
        {
            _core.OnDetachedForDisposable(owner, this);
        }

        public void Dispose()
        {
            if(!_disposed) {
                _disposed = true;
                GC.SuppressFinalize(this);
                _vertexCountArray.Dispose();
                _textureIndexArray.Dispose();

                var textures = _textures.Span;
                for(int i = 0; i < textures.Length; i++) {
                    TextureObject.Delete(ref textures[i]);
                }
                _textures.Dispose();
            }
        }
    }
}
