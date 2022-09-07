#nullable enable
using Elffy;
using Elffy.Components.Implementation;
using Elffy.Features;
using Elffy.Graphics.OpenGL;
using System;

namespace Elffy.Components
{
    public class ArrayTexture : ISingleOwnerComponent
    {
        private SingleOwnerComponentCore _core;             // Mutable object, Don't change into readonly
        private TextureConfig _config;
        private TextureObject _to;
        private Vector2i _size;
        private int _count;

        public ComponentOwner? Owner => _core.Owner;

        public bool IsEmpty => _to.IsEmpty;

        public TextureObject TextureObject => _to;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        public int Width => _size.X;

        public int Height => _size.Y;

        public int Count => _count;

        public ArrayTexture(in TextureConfig config)
        {
            _config = config;
            _to = TextureObject.Empty;
            _size = Vector2i.Zero;
            _count = 0;
            _core = new SingleOwnerComponentCore();
        }

        ~ArrayTexture() => Dispose(false);

        public unsafe void Load(Vector2i size, int count, ReadOnlySpan<ColorByte> data)
        {
            if(size.X * size.Y * count != data.Length) {
                throw new ArgumentException($"Length of data is invalid, which must be size.X * size.Y * count.");
            }
            fixed(ColorByte* ptr = data) {
                Load(size, count, ptr);
            }
        }

        public unsafe void Load(Vector2i size, int count, ColorByte* data)
        {
            _to = TextureObject.Create();
            TextureObject.Bind2DArray(_to);

            TextureObject.Parameter2DArrayMinFilter(_config.ShrinkMode, _config.MipmapMode);
            TextureObject.Parameter2DArrayMagFilter(_config.ExpansionMode);
            TextureObject.Parameter2DArrayWrapS(_config.WrapModeX);
            TextureObject.Parameter2DArrayWrapT(_config.WrapModeY);

            TextureObject.Image2DArray(size, count, data, 0);

            if(_config.MipmapMode != TextureMipmapMode.None) {
                TextureObject.GenerateMipmap2DArray();
            }
            TextureObject.Unbind2DArray();
            _size = size;
            _count = count;

            ContextAssociatedMemorySafety.Register(this, Engine.GetValidCurrentContext());
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing) {
                TextureObject.Delete(ref _to);
                _size = default;
                _count = 0;
            }
            else {
                ContextAssociatedMemorySafety.OnFinalized(this);
            }
        }

        void IComponent.OnAttached(ComponentOwner owner) => _core.OnAttached(owner, this);

        void IComponent.OnDetached(ComponentOwner owner) => _core.OnDetached(owner, this);
    }
}
