#nullable enable
using Elffy.Components.Implementation;
using Elffy.Core;
using Elffy.Effective;
using Elffy.Graphics.OpenGL;
using System;
using System.Diagnostics;

namespace Elffy.Components
{
    internal sealed class PmxModelParts : ISingleOwnerComponent
    {
        private SingleOwnerComponentCore _core;

        private bool _disposed;
        private ValueTypeRentMemory<int> _vertexCountArray;
        private ValueTypeRentMemory<int> _textureIndexArray;
        private ValueTypeRentMemory<TextureObject> _textures;

        public ComponentOwner? Owner => _core.Owner;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        public ReadOnlySpan<int> VertexCountArray => _vertexCountArray.Span;

        public ReadOnlySpan<int> TextureIndexArray => _textureIndexArray.Span;

        public ReadOnlySpan<TextureObject> Textures => _textures.Span;

        public int Current { get; set; }

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

        ~PmxModelParts() => Dispose(false);

        void IComponent.OnAttached(ComponentOwner owner) => _core.OnAttached(owner, this);

        void IComponent.OnDetached(ComponentOwner owner) => _core.OnDetached(owner, this);

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(!_disposed) {
                _disposed = true;
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
