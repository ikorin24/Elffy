#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Elffy.Effective.Unsafes;
using Elffy.Graphics.OpenGL;
using Elffy.Imaging;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Features.Implementation
{
    public struct TextureCore : IDisposable
    {
        private TextureConfig _config;
        private Vector2i _size;
        private TextureObject _texture;

        public TextureObject Texture => _texture;
        public Vector2i Size => _size;
        public TextureExpansionMode ExpansionMode => _config.ExpansionMode;
        public TextureShrinkMode ShrinkMode => _config.ShrinkMode;
        public TextureMipmapMode MipmapMode => _config.MipmapMode;
        public TextureWrap WrapModeX => _config.WrapModeX;
        public TextureWrap WrapModeY => _config.WrapModeY;

        public bool IsEmpty => Texture.IsEmpty;


        public TextureCore(in TextureConfig config)
        {
            _config = config;
            _texture = TextureObject.Empty;
            _size = Vector2i.Zero;
        }

        public void Load<T>(T state, in Vector2i size, ImageBuilderDelegate<T> imageBuilder)
        {
            if(!Texture.IsEmpty) {
                ThrowAlreadyLoaded();
            }
            _texture = TextureLoadHelper.LoadByDMA(state, size, imageBuilder,
                                                  ExpansionMode, ShrinkMode,
                                                  MipmapMode, WrapModeX, WrapModeY);
            _size = size;
        }

        public void Load(in ReadOnlyImageRef image)
        {
            if(!Texture.IsEmpty) {
                ThrowAlreadyLoaded();
            }
            _texture = TextureLoadHelper.LoadByDMA(image, ExpansionMode, ShrinkMode, MipmapMode, WrapModeX, WrapModeY);
            _size = new(image.Width, image.Height);
        }

        public void Load(in Vector2i size, ReadOnlySpan<ColorByte> pixels)
        {
            var image = new ReadOnlyImageRef(pixels, size.X, size.Y);
            Load(image);
        }

        public unsafe void Load(in Vector2i size, in ColorByte fill)
        {
            if(!Texture.IsEmpty) {
                ThrowAlreadyLoaded();
            }
            using var pixels = new UnsafeRawArray<ColorByte>(size.X * size.Y, false);
            pixels.AsSpan().Fill(fill);
            LoadCore<ColorByte>(size, pixels.GetPtr());
        }

        public unsafe void Load(in Vector2i size, in Color4 fill)
        {
            if(!Texture.IsEmpty) {
                ThrowAlreadyLoaded();
            }
            using var pixels = new UnsafeRawArray<Color4>(size.X * size.Y, false);
            pixels.AsSpan().Fill(fill);
            LoadCore<Color4>(size, pixels.GetPtr());
        }

        public unsafe void Load(in Vector2i size, ReadOnlySpan<Color4> pixels)
        {
            if(size.X * size.Y != pixels.Length) {
                Throw();
                [DoesNotReturn] static void Throw() => throw new ArgumentException("invalid size");
            }
            if(!Texture.IsEmpty) {
                ThrowAlreadyLoaded();
            }
            fixed(Color4* p = pixels) {
                LoadCore<Color4>(in size, p);
            }
        }

        public unsafe void LoadUndefined(in Vector2i size)
        {
            if(!Texture.IsEmpty) {
                ThrowAlreadyLoaded();
            }
            LoadCore<ColorByte>(size, null);
        }

        public unsafe void Update(in Vector2i offset, in ReadOnlyImageRef subImage)
        {
            if(Texture.IsEmpty) {
                ThrowEmptyTexture();
            }
            // Requirements
            // 0 <= offset.X < Size.X
            // 0 <= offset.Y < Size.Y
            // 0 <= subImage.Width <= Size.X - offset.X     (if subImage.Width == 0, do nothing)
            // 0 <= subImage.Height <= Size.Y - offset.Y    (if subImage.Height == 0, do nothing)

            if((uint)offset.X >= (uint)Size.X || (uint)offset.Y >= (uint)Size.Y) {
                ThrowOutOfRange(nameof(offset));
            }
            if(subImage.IsEmpty) { return; }
            if(subImage.Width > Size.X || subImage.Height > Size.Y) {
                ThrowOutOfRange($"Rect to update is larger than texture size.");
            }

            fixed(ColorByte* ptr = subImage) {
                UpdateSubTexture(Texture, ptr, new RectI(offset.X, offset.Y, subImage.Width, subImage.Height));
            }


            [DoesNotReturn] static void ThrowOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);
        }

        public unsafe void Update(in RectI rect, ReadOnlySpan<ColorByte> pixels)
        {
            if(Texture.IsEmpty) {
                ThrowEmptyTexture();
            }
            if(rect.X < 0 || rect.Y < 0 || (uint)rect.Width >= (uint)(Size.X - rect.X) || (uint)rect.Height >= (uint)(Size.Y - rect.Y)) {
                ThrowInvalidRect();
                [DoesNotReturn] static void ThrowInvalidRect() => throw new ArgumentOutOfRangeException($"{nameof(rect)} is invalid");
            }

            fixed(ColorByte* ptr = pixels) {
                UpdateSubTexture(Texture, ptr, rect);
            }
        }

        public unsafe void Update(in RectI rect, ReadOnlySpan<Color4> pixels)
        {
            if(Texture.IsEmpty) {
                ThrowEmptyTexture();
            }
            if(rect.X < 0 || rect.Y < 0 || (uint)rect.Width >= (uint)(Size.X - rect.X) || (uint)rect.Height >= (uint)(Size.Y - rect.Y)) {
                ThrowInvalidRect();
                [DoesNotReturn] static void ThrowInvalidRect() => throw new ArgumentOutOfRangeException($"{nameof(rect)} is invalid");
            }

            fixed(Color4* ptr = pixels) {
                UpdateSubTexture(Texture, ptr, rect);
            }
        }

        public unsafe void Update(in RectI rect, in ColorByte fill)
        {
            if(Texture.IsEmpty) {
                ThrowEmptyTexture();
            }
            if(rect.X < 0 || rect.Y < 0 || (uint)rect.Width >= (uint)(Size.X - rect.X) || (uint)rect.Height >= (uint)(Size.Y - rect.Y)) {
                ThrowInvalidRect();
                [DoesNotReturn] static void ThrowInvalidRect() => throw new ArgumentOutOfRangeException($"{nameof(rect)} is invalid");
            }

            using var pixels = new UnsafeRawArray<ColorByte>(rect.Width * rect.Height, false);
            pixels.AsSpan().Fill(fill);
            UpdateSubTexture(Texture, pixels.GetPtr(), rect);
        }

        internal static unsafe void UpdateSubTexture(in TextureObject to, ColorByte* pixels, in RectI rect)
        {
            if(to.IsEmpty) {
                ThrowEmptyTexture();
            }
            TextureObject.Bind2D(to);
            TextureObject.SubImage2D(rect, pixels, 0);
            TextureObject.Unbind2D();
        }

        internal static unsafe void UpdateSubTexture(in TextureObject to, Color4* pixels, in RectI rect)
        {
            if(to.IsEmpty) {
                ThrowEmptyTexture();
            }
            TextureObject.Bind2D(to);
            TextureObject.SubImage2D(rect, pixels, 0);
            TextureObject.Unbind2D();
        }

        public void GetPixels(in Vector2i offset, in ImageRef dest) => GetPixels(new RectI(offset, dest.Size), dest.GetPixels());

        public unsafe int GetPixels(in RectI rect, Span<ColorByte> buffer)
        {
            if(Texture.IsEmpty) {
                return 0;
            }
            if((uint)rect.X >= (uint)Size.X ||
               (uint)rect.Y >= (uint)Size.Y ||
               (uint)rect.Width > (uint)(Size.X - rect.X) ||
               (uint)rect.Height > (uint)(Size.Y - rect.Y)) {
                throw new ArgumentOutOfRangeException(nameof(rect));
            }
            var len = rect.X * rect.Y * sizeof(ColorByte);
            if(buffer.Length < len) {
                throw new ArgumentException(nameof(buffer));
            }

            using(var _ = FBO.PreserveCurrentBinded()) {
                var fbo = FBO.Create();
                try {
                    FBO.Bind(fbo, FBO.Target.FrameBuffer);
                    FBO.SetTexture2DColorAttachment(Texture, 0);
                    FBO.ThrowIfInvalidStatus();
                    fixed(void* ptr = buffer) {
                        GL.ReadPixels(rect.X, rect.Y, rect.Width, rect.Height, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)ptr);
                    }
                }
                finally {
                    FBO.Unbind(FBO.Target.FrameBuffer);
                    FBO.Delete(ref fbo);
                }
                return len;
            }
        }

        internal static unsafe int GetPixels(in TextureObject to, in RectI rect, Span<ColorByte> buffer)
        {
            // I don't check that rect is valid here.
            if(to.IsEmpty) {
                return 0;
            }
            var len = rect.X * rect.Y * sizeof(ColorByte);
            if(buffer.Length < len) {
                throw new ArgumentException(nameof(buffer));
            }

            using(var _ = FBO.PreserveCurrentBinded()) {
                var fbo = FBO.Create();
                try {
                    FBO.Bind(fbo, FBO.Target.FrameBuffer);
                    FBO.SetTexture2DColorAttachment(to, 0);
                    FBO.ThrowIfInvalidStatus();
                    fixed(void* ptr = buffer) {
                        GL.ReadPixels(rect.X, rect.Y, rect.Width, rect.Height, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)ptr);
                    }
                }
                finally {
                    FBO.Unbind(FBO.Target.FrameBuffer);
                    FBO.Delete(ref fbo);
                }
                return len;
            }
        }

        public void Dispose()
        {
            TextureObject.Delete(ref _texture);
            _size = default;
        }

        private unsafe void LoadCore<TColor>(in Vector2i size, TColor* pixels) where TColor : unmanaged
        {
            if(typeof(TColor) != typeof(ColorByte) && typeof(TColor) != typeof(Color4)) {
                ThrowNotSupported();
                [DoesNotReturn] static void ThrowNotSupported() => throw new NotSupportedException();
            }

            _texture = TextureObject.Create();
            TextureObject.Bind2D(Texture);
            TextureObject.Parameter2DMinFilter(ShrinkMode, MipmapMode);
            TextureObject.Parameter2DMagFilter(ExpansionMode);
            TextureObject.Parameter2DWrapS(WrapModeX);
            TextureObject.Parameter2DWrapT(WrapModeY);

            if(typeof(TColor) == typeof(ColorByte)) {
                TextureObject.Image2D(size, (ColorByte*)pixels, 0);
            }
            else if(typeof(TColor) == typeof(Color4)) {
                TextureObject.Image2D(size, (Color4*)pixels, 0);
            }

            if(MipmapMode != TextureMipmapMode.None) {
                TextureObject.GenerateMipmap2D();
            }
            TextureObject.Unbind2D();
            _size = size;
        }

        [DoesNotReturn]
        static void ThrowAlreadyLoaded() => throw new InvalidOperationException("Texture is already loaded.");

        [DoesNotReturn]
        static void ThrowEmptyTexture() => throw new InvalidOperationException("Texture is empty");
    }
}
