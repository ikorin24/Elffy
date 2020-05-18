#nullable enable
using System;
using System.Drawing;
using Elffy.Core;

namespace Elffy.Components
{
    public sealed class MultiTexture : IComponent, IDisposable, IComponentInternal<MultiTexture>
    {
        private bool _isLoaded;
        private bool _disposed;
        private Texture[]? _textures;

        public int Count => _textures?.Length ?? 0;

        MultiTexture IComponentInternal<MultiTexture>.Self => this;

        ~MultiTexture() => Dispose(false);

        public void Apply(int index)
        {
            if(_textures != null) {
                _textures[index].Apply();
            }
        }

        public void Load(ReadOnlySpan<Bitmap> bitmaps)
        {
            if(_isLoaded) { throw new InvalidOperationException("Already loaded"); }
            var textures = new Texture[bitmaps.Length];
            for(int i = 0; i < textures.Length; i++) {
                textures[i] = new Texture(TextureExpansionMode.Bilinear, TextureShrinkMode.Bilinear, TextureMipmapMode.None);
                textures[i].Load(bitmaps[i]);
            }
            _textures = textures;
            _isLoaded = true;
        }

        public void Dispose()
        {
            if(_disposed) { return; }
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(disposing) {
                var textures = _textures;
                if(textures != null) {
                    foreach(var texture in textures) {
                        texture.Dispose();
                    }
                    _textures = null;
                }
                _disposed = true;
            }
        }

        public void OnAttached(ComponentOwner owner) { }

        public void OnDetached(ComponentOwner owner) { }
    }
}
