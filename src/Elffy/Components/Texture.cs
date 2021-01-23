#nullable enable
using System;
using System.Drawing;
using Elffy.Core;
using Elffy.Exceptions;
using Elffy.OpenGL;

namespace Elffy.Components
{
    public sealed class Texture : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<Texture> _core;    // Mutable object, Don't change into readonly
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

        public ref readonly Vector2i Size => ref _textureCore.Size;

        public Texture(bool autoDispose = true) : this(TextureWrapMode.ClampToEdge, autoDispose)
        {
        }

        public Texture(TextureWrapMode wrapMode, bool autoDispose = true)
            : this(TextureExpansionMode.Bilinear, TextureShrinkMode.Bilinear, TextureMipmapMode.Bilinear, wrapMode, wrapMode, autoDispose)
        {
        }

        public Texture(TextureExpansionMode expansionMode, TextureShrinkMode shrinkMode,
                       TextureMipmapMode mipmapMode, TextureWrapMode wrapModeX, TextureWrapMode wrapModeY,
                       bool autoDispose = true)
        {
            _textureCore = new(expansionMode, shrinkMode, mipmapMode, wrapModeX, wrapModeY);
            _core = new(autoDispose);
        }

        ~Texture() => Dispose(false);

        /// <summary>Load pixel data from <see cref="Bitmap"/></summary>
        /// <remarks>Texture width and height should be power of two for performance.</remarks>
        /// <param name="bitmap">bitmap to load pixels</param>
        public void Load(Bitmap bitmap) => _textureCore.Load(bitmap);

        /// <summary>Load specified pixel data with specified texture size</summary>
        /// <remarks>Texture width and height should be power of two for performance.</remarks>
        /// <param name="size">texture size</param>
        /// <param name="pixels">pixel data</param>
        public void Load(in Vector2i size, ReadOnlySpan<ColorByte> pixels) => _textureCore.Load(size, pixels);

        /// <summary>Load pixel data filled with specified color</summary>
        /// <remarks>Texture width and height should be power of two for performance.</remarks>
        /// <param name="size">texture size</param>
        /// <param name="fill">color to fill all pixels with</param>
        public unsafe void Load(in Vector2i size, in ColorByte fill) => _textureCore.Load(size, fill);

        /// <summary>Create gpu texture buffer with specified size, but no uploading pixels. Pixels color remain undefined.</summary>
        /// <remarks>Texture width and height should be power of two for performance.</remarks>
        /// <param name="size">texture size</param>
        public void LoadUndefined(in Vector2i size) => _textureCore.LoadUndefined(size);

        public void Update(in RectI rect, ReadOnlySpan<ColorByte> pixels) => _textureCore.Update(rect, pixels);

        public void Update(in RectI rect, in ColorByte fill) => _textureCore.Update(rect, fill);

        public int GetPixels(in RectI rect, Span<ColorByte> buffer) => _textureCore.GetPixels(rect, buffer);

        public TexturePainter GetPainter(bool copyFromOriginal = true) => _textureCore.GetPainter(copyFromOriginal);

        public TexturePainter GetPainter(in RectI rect, bool copyFromOriginal = true) => _textureCore.GetPainter(rect, copyFromOriginal);

        void IComponent.OnAttached(ComponentOwner owner) => _core.OnAttached(owner);

        void IComponent.OnDetached(ComponentOwner owner) => _core.OnDetachedForDisposable(owner, this);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(disposing) {
                _textureCore.Dispose();
            }
            else {
                // Cannot release objects of OpenGL from the finalizer thread.
                throw new MemoryLeakException(typeof(Texture));
            }
        }
    }
}
