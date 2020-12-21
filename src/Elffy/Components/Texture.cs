#nullable enable
using System;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.OpenGL;
using OpenTK.Graphics.OpenGL4;
using TKPixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace Elffy.Components
{
    public sealed class Texture : ISingleOwnerComponent, IDisposable
    {
        // I use GL_RGBA as internal pixel format (format in GPU).
        //
        // |   0    |   1    |   2    |   3    |
        // |   R    |   G    |   B    |   A    |
        // | 1 byte | 1 byte | 1 byte | 1 byte |
        // |             4 bytes               |
        //
        // The driver of opengl convert it if we send pixels as other formats.
        // Avoid that if possible.
        private PixelInternalFormat InternalFormat = PixelInternalFormat.Rgba;


        private SingleOwnerComponentCore<Texture> _core;    // Mutable object, Don't change into readonly
        private TextureObject _to;
        private Vector2i _size;
        public TextureExpansionMode ExpansionMode { get; }
        public TextureShrinkMode ShrinkMode { get; }
        public TextureMipmapMode MipmapMode { get; }
        public TextureWrapMode WrapModeX { get; }
        public TextureWrapMode WrapModeY { get; }

        public TextureObject TextureObject => _to;

        public ComponentOwner? Owner => _core.Owner;

        public bool IsEmpty => _to.IsEmpty;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        public ref readonly Vector2i Size => ref _size;

        public Texture(TextureWrapMode wrapMode, bool autoDispose = true)
            : this(TextureExpansionMode.Bilinear, TextureShrinkMode.Bilinear, TextureMipmapMode.Bilinear, wrapMode, wrapMode, autoDispose)
        {
        }

        public Texture(TextureExpansionMode expansionMode, TextureShrinkMode shrinkMode,
                       TextureMipmapMode mipmapMode, TextureWrapMode wrapModeX, TextureWrapMode wrapModeY,
                       bool autoDispose = true)
        {
            ExpansionMode = expansionMode;
            ShrinkMode = shrinkMode;
            MipmapMode = mipmapMode;
            WrapModeX = wrapModeX;
            WrapModeY = wrapModeY;
            _core = new SingleOwnerComponentCore<Texture>(autoDispose);
        }

        ~Texture() => Dispose(false);

        /// <summary>Load pixel data from <see cref="Bitmap"/></summary>
        /// <remarks>Texture width and height should be power of two for performance.</remarks>
        /// <param name="bitmap">bitmap to load pixels</param>
        public unsafe void Load(Bitmap bitmap)
        {
            if(!_to.IsEmpty) {
                ThrowInvalidOperation();
                [DoesNotReturn] static void ThrowInvalidOperation() => throw new InvalidOperationException("Texture is already loaded.");
            }
            if(bitmap is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(bitmap));
            }

            _to = TextureObject.Create();
            TextureObject.Bind2D(_to);
            TextureObject.Parameter2DMinFilter(ShrinkMode, MipmapMode);
            TextureObject.Parameter2DMagFilter(ExpansionMode);
            TextureObject.Parameter2DWrapS(WrapModeX);
            TextureObject.Parameter2DWrapT(WrapModeY);
            TextureObject.Image2D(bitmap);
            if(MipmapMode != TextureMipmapMode.None) {
                TextureObject.GenerateMipmap2D();
            }
            TextureObject.Unbind2D();
            _size = new(bitmap.Width, bitmap.Height);
        }

        /// <summary>Load specified pixel data with specified texture size</summary>
        /// <remarks>Texture width and height should be power of two for performance.</remarks>
        /// <param name="size">texture size</param>
        /// <param name="pixels">pixel data</param>
        public unsafe void Load(in Vector2i size, ReadOnlySpan<ColorByte> pixels)
        {
            if(!_to.IsEmpty) {
                ThrowInvalidOperation();
                [DoesNotReturn] static void ThrowInvalidOperation() => throw new InvalidOperationException("Texture is already loaded.");
            }
            if(size.X <= 0 || size.Y <= 0) {
                ThrowInvalidSize();
                [DoesNotReturn] static void ThrowInvalidSize() => throw new ArgumentOutOfRangeException($"{nameof(size)} is invalid");
            }
            if(pixels.Length < size.X * size.Y) {
                ThrowPixelsTooShort();
                [DoesNotReturn] static void ThrowPixelsTooShort() => throw new ArgumentException($"{nameof(pixels)} is too short");
            }
            fixed(ColorByte* ptr = pixels) {
                LoadCore(size, ptr);
            }
        }

        /// <summary>Load pixel data filled with specified color</summary>
        /// <remarks>Texture width and height should be power of two for performance.</remarks>
        /// <param name="size">texture size</param>
        /// <param name="fill">color to fill all pixels with</param>
        public unsafe void Load(in Vector2i size, in ColorByte fill)
        {
            if(!_to.IsEmpty) {
                ThrowInvalidOperation();
                [DoesNotReturn] static void ThrowInvalidOperation() => throw new InvalidOperationException("Texture is already loaded.");
            }
            if(size.X <= 0 || size.Y <= 0) {
                ThrowInvalidSize();
                [DoesNotReturn] static void ThrowInvalidSize() => throw new ArgumentOutOfRangeException($"{nameof(size)} is invalid");
            }
            using(var buf = new PooledArray<byte>(size.X * size.Y * sizeof(ColorByte))) {
                var pixels = buf.AsSpan().MarshalCast<byte, ColorByte>();
                pixels.Fill(fill);
                fixed(ColorByte* ptr = pixels) {
                    LoadCore(size, ptr);
                }
            }
        }

        /// <summary>Create gpu texture buffer with specified size, but no uploading pixels. Pixels color remain undefined.</summary>
        /// <remarks>Texture width and height should be power of two for performance.</remarks>
        /// <param name="size">texture size</param>
        public unsafe void LoadUndefined(in Vector2i size)
        {
            if(!_to.IsEmpty) {
                ThrowAlreadyLoaded();
                static void ThrowAlreadyLoaded() => throw new InvalidOperationException("Texture is already loaded.");
            }
            LoadCore(size, null);
        }

        public unsafe void Update(in RectI rect, ReadOnlySpan<ColorByte> pixels)
        {
            if(_to.IsEmpty) {
                ThrowNotLoaded();
                static void ThrowNotLoaded() => throw new InvalidOperationException("Texture is not loaded yet.");
            }
            if(rect.X < 0 || rect.Y < 0 || (uint)rect.Width >= (uint)(_size.X - rect.X) || (uint)rect.Height >= (uint)(_size.Y - rect.Y)) {
                ThrowInvalidRect();
                [DoesNotReturn] static void ThrowInvalidRect() => throw new ArgumentOutOfRangeException($"{nameof(rect)} is invalid");
            }

            fixed(ColorByte* ptr = pixels) {
                UpdateSubTexture(ptr, rect);
            }
        }

        public unsafe void Update(in RectI rect, in ColorByte fill)
        {
            if(_to.IsEmpty) {
                ThrowNotLoaded();
                static void ThrowNotLoaded() => throw new InvalidOperationException("Texture is not loaded yet.");
            }
            if(rect.X < 0 || rect.Y < 0 || (uint)rect.Width >= (uint)(_size.X - rect.X) || (uint)rect.Height >= (uint)(_size.Y - rect.Y)) {
                ThrowInvalidRect();
                [DoesNotReturn] static void ThrowInvalidRect() => throw new ArgumentOutOfRangeException($"{nameof(rect)} is invalid");
            }

            using(var buf = new PooledArray<byte>(rect.Width * rect.Height * sizeof(ColorByte))) {
                var pixels = buf.AsSpan().MarshalCast<byte, ColorByte>();
                pixels.Fill(fill);
                fixed(ColorByte* ptr = pixels) {
                    UpdateSubTexture(ptr, rect);
                }
            }
        }

        public unsafe int GetPixels(in RectI rect, Span<ColorByte> buffer)
        {
            if(_to.IsEmpty) {
                return 0;
            }
            if((uint)rect.X >= (uint)_size.X ||
               (uint)rect.Y >= (uint)_size.Y ||
               (uint)rect.Width > (uint)(_size.X - rect.X) ||
               (uint)rect.Height > (uint)(_size.Y - rect.Y)) {
                throw new ArgumentOutOfRangeException();
            }
            var len = rect.X * rect.Y * sizeof(ColorByte);
            if(buffer.Length < len) {
                throw new ArgumentException($"{nameof(buffer)} is too short.");
            }

            // Get current binded fbo
            var currentRead = FBO.CurrentReadBinded;
            var currentDraw = FBO.CurrentDrawBinded;

            var fbo = FBO.Create();
            try {
                FBO.Bind(fbo, FBO.Target.FrameBuffer);
                FBO.SetTexture2DBuffer(_to, FBO.Attachment.ColorAttachment0);
                if(!FBO.CheckStatus(out var error)) {
                    throw new Exception(error);
                }
                fixed(void* ptr = buffer) {
                    GL.ReadPixels(rect.X, rect.Y, rect.Width, rect.Height, TKPixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)ptr);
                }
            }
            finally {
                FBO.Unbind(FBO.Target.FrameBuffer);
                FBO.Delete(ref fbo);

                // Restore binded
                FBO.Bind(currentRead, FBO.Target.Read);
                FBO.Bind(currentDraw, FBO.Target.Draw);
            }
            return len;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TexturePainter GetPainter(bool copyFromOriginal = true)
        {
            if(_to.IsEmpty) {
                ThrowEmptyTexture();
                static void ThrowEmptyTexture() => throw new InvalidOperationException("Texture is not loaded yet.");
            }

            return new TexturePainter(this, new RectI(0, 0, _size.X, _size.Y), copyFromOriginal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TexturePainter GetPainter(in RectI rect, bool copyFromOriginal = true)
        {
            if(_to.IsEmpty) {
                ThrowEmptyTexture();
                static void ThrowEmptyTexture() => throw new InvalidOperationException("Texture is not loaded yet.");
            }

            if((uint)rect.X >= (uint)_size.X) { ThrowOutOfRange(); }
            if((uint)rect.Y >= (uint)_size.Y) { ThrowOutOfRange(); }
            if((uint)rect.Width > (uint)(_size.X - rect.X)) { ThrowOutOfRange(); }
            if((uint)rect.Height > (uint)(_size.Y - rect.Y)) { ThrowOutOfRange(); }

            return new TexturePainter(this, rect, copyFromOriginal);

            static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException();
        }


        private unsafe void LoadCore(in Vector2i size, ColorByte* pixels)
        {
            _to = TextureObject.Create();
            TextureObject.Bind2D(_to);
            TextureObject.Parameter2DMinFilter(ShrinkMode, MipmapMode);
            TextureObject.Parameter2DMagFilter(ExpansionMode);
            TextureObject.Parameter2DWrapS(WrapModeX);
            TextureObject.Parameter2DWrapT(WrapModeY);
            TextureObject.Image2D(size, pixels);
            if(MipmapMode != TextureMipmapMode.None) {
                TextureObject.GenerateMipmap2D();
            }
            TextureObject.Unbind2D();
            _size = size;
        }

        internal unsafe void UpdateSubTexture(ColorByte* pixels, in RectI rect)
        {
            // This method is called from TexturePainter

            Debug.Assert(_to.IsEmpty == false);
            TextureObject.Bind2D(_to);
            TextureObject.SubImage2D(rect, pixels);
            TextureObject.Unbind2D();
        }

        void IComponent.OnAttached(ComponentOwner owner) => _core.OnAttached(owner);

        void IComponent.OnDetached(ComponentOwner owner) => _core.OnDetachedForDisposable(owner, this);

        public void Dispose()
        {
            if(_to.IsEmpty) { return; }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(_to.IsEmpty) { return; }
            if(disposing) {
                TextureObject.Delete(ref _to);
                _size = default;
            }
            else {
                // Cannot release objects of OpenGL from the finalizer thread.
                throw new MemoryLeakException(typeof(Texture));
            }
        }
    }
}
