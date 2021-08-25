#nullable enable
using System;
using Elffy.Core;
using Elffy.OpenGL;
using Elffy.Imaging;
using Elffy.Components.Implementation;

namespace Elffy.Components
{
    public class Texture : ISingleOwnerComponent
    {
        private SingleOwnerComponentCore _core;             // Mutable object, Don't change into readonly
        private TextureCore _textureCore;                   // Mutable object, Don't change into readonly

        public TextureExpansionMode ExpansionMode => _textureCore.ExpansionMode;
        public TextureShrinkMode ShrinkMode => _textureCore.ShrinkMode;
        public TextureMipmapMode MipmapMode => _textureCore.MipmapMode;
        public TextureWrapMode WrapModeX => _textureCore.WrapModeX;
        public TextureWrapMode WrapModeY => _textureCore.WrapModeY;

        public ref readonly TextureObject TextureObject => ref _textureCore.Texture;

        public ComponentOwner? Owner => _core.Owner;

        public bool IsEmpty => _textureCore.IsEmpty;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        public int Width => _textureCore.Size.X;

        public int Height => _textureCore.Size.Y;

        public ref readonly Vector2i Size => ref _textureCore.Size;

        public Texture(in TextureConfig config)
        {
            _textureCore = new TextureCore(config);
            _core = new SingleOwnerComponentCore();
        }

        ~Texture() => Dispose(false);

        public void Load<T>(in Vector2i size, ImageBuilderDelegate<T> imageBuilder, T state)
        {
            _textureCore.Load(state, size, imageBuilder);
            ContextAssociatedMemorySafety.Register(this, Engine.CurrentContext!);
        }

        /// <summary>Load image</summary>
        /// <param name="image">image to load</param>
        public void Load(in ReadOnlyImageRef image)
        {
            _textureCore.Load(image);
            ContextAssociatedMemorySafety.Register(this, Engine.CurrentContext!);
        }

        /// <summary>Load pixel data filled with specified color</summary>
        /// <remarks>Texture width and height should be power of two for performance.</remarks>
        /// <param name="size">texture size</param>
        /// <param name="fill">color to fill all pixels with</param>
        public unsafe void Load(in Vector2i size, in ColorByte fill)
        {
            _textureCore.Load(size, fill);
            ContextAssociatedMemorySafety.Register(this, Engine.CurrentContext!);
        }

        /// <summary>Create gpu texture buffer with specified size, but no uploading pixels. Pixels color remain undefined.</summary>
        /// <remarks>Texture width and height should be power of two for performance.</remarks>
        /// <param name="size">texture size</param>
        public void LoadUndefined(in Vector2i size)
        {
            _textureCore.LoadUndefined(size);
            ContextAssociatedMemorySafety.Register(this, Engine.CurrentContext!);
        }

        public void Update(in Vector2i offset, in ReadOnlyImageRef subImage) => _textureCore.Update(offset, subImage);
        //public void Update(in RectI rect, ReadOnlySpan<ColorByte> pixels) => _textureCore.Update(rect, pixels);

        public void Update(in RectI rect, in ColorByte fill) => _textureCore.Update(rect, fill);

        public int GetPixels(in RectI rect, Span<ColorByte> buffer) => _textureCore.GetPixels(rect, buffer);

        public TexturePainter GetPainter(bool copyFromOriginal = true) => _textureCore.GetPainter(copyFromOriginal);

        public TexturePainter GetPainter(in RectI rect, bool copyFromOriginal = true) => _textureCore.GetPainter(rect, copyFromOriginal);

        void IComponent.OnAttached(ComponentOwner owner) => _core.OnAttached(owner, this);

        void IComponent.OnDetached(ComponentOwner owner) => _core.OnDetached(owner, this);

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing) {
                _textureCore.Dispose();
            }
            else {
                ContextAssociatedMemorySafety.OnFinalized(this);
            }
        }
    }
}
