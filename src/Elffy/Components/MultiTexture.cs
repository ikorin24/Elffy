#nullable enable
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.OpenGL;

namespace Elffy.Components
{
    public sealed class MultiTexture : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<MultiTexture> _core = new SingleOwnerComponentCore<MultiTexture>(true);
        private bool _disposed;
        private RefTypeRentMemory<Texture> _textures;
        private int _count;
        private int _current;

        public int Count => _count;
        public int Current
        {
            get => _current;
            set
            {
                if((uint)value >= (uint)_count) { ThrowOutOfRange(); }
                _current = value;

                static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value));
            }
        }


        public ComponentOwner? Owner => _core.Owner;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        ~MultiTexture() => Dispose(false);

        public void Apply(TextureUnitNumber textureUnit)
        {
            if(_textures.IsEmpty) { return; }
            _textures.Span[_current].Apply(textureUnit);
        }

        public void Load(ReadOnlySpan<Bitmap> bitmaps)
        {
            if(!_textures.IsEmpty) {
                ClearTextures();
            }

            if(bitmaps.Length == 0) { return; }

            var textures = new RefTypeRentMemory<Texture>(bitmaps.Length);
            try {
                var span = textures.Span;
                for(int i = 0; i < span.Length; i++) {
                    span[i] = new Texture(TextureExpansionMode.Bilinear, TextureShrinkMode.Bilinear, TextureMipmapMode.None);
                    span[i].Load(bitmaps[i]);
                }
            }
            catch(Exception) {
                textures.Dispose();
                throw;
            }
            _textures = textures;
            _count = bitmaps.Length;
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
                ClearTextures();
                _current = 0;
                _disposed = true;
            }
            else {
                throw new MemoryLeakException(typeof(MultiTexture));
            }
        }

        private void ClearTextures()
        {
            foreach(var texture in _textures.Span) {
                texture.Dispose();
            }
            _textures.Dispose();
            _textures = default;
            _count = 0;
        }

        public void OnAttached(ComponentOwner owner) => _core.OnAttached(owner);

        public void OnDetached(ComponentOwner owner) => _core.OnDetachedForDisposable(owner, this);
    }
}
