#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.OpenGL;
using Elffy.Imaging;
using Elffy.Core;
using OpenTK.Graphics.OpenGL4;
using TKPixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace Elffy.Components
{
    internal struct TextureCore : IDisposable
    {
        public TextureObject Texture;
        public Vector2i Size;
        public TextureExpansionMode ExpansionMode { get; }
        public TextureShrinkMode ShrinkMode { get; }
        public TextureMipmapMode MipmapMode { get; }
        public TextureWrapMode WrapModeX { get; }
        public TextureWrapMode WrapModeY { get; }

        public bool IsEmpty => Texture.IsEmpty;

        public TextureCore(TextureExpansionMode expansionMode, TextureShrinkMode shrinkMode,
                           TextureMipmapMode mipmapMode, TextureWrapMode wrapModeX, TextureWrapMode wrapModeY)
        {
            ExpansionMode = expansionMode;
            ShrinkMode = shrinkMode;
            MipmapMode = mipmapMode;
            WrapModeX = wrapModeX;
            WrapModeY = wrapModeY;
            Texture = TextureObject.Empty;
            Size = Vector2i.Zero;
        }

        public void Load<T>(T state, in Vector2i size, ImageBuilderDelegate<T> imageBuilder)
        {
            if(!Texture.IsEmpty) {
                ThrowAlreadyLoaded();
            }
            Texture = TextureLoadHelper.LoadByDMA(state, size, imageBuilder, 
                                                  ExpansionMode, ShrinkMode, 
                                                  MipmapMode, WrapModeX, WrapModeY);
            Size = size;
        }

        public void Load(in ReadOnlyImageRef image)
        {
            if(!Texture.IsEmpty) {
                ThrowAlreadyLoaded();
            }
            Texture = TextureLoadHelper.LoadByDMA(image, ExpansionMode, ShrinkMode, MipmapMode, WrapModeX, WrapModeY);
            Size = new(image.Width, image.Height);
        }

        [Obsolete("obsolete", true)]
        public unsafe void Load(in Vector2i size, ReadOnlySpan<Color4> pixels)
        {
            if(!Texture.IsEmpty) {
                ThrowAlreadyLoaded();
            }
            if(size.X <= 0 || size.Y <= 0) {
                ThrowInvalidSize();
                [DoesNotReturn] static void ThrowInvalidSize() => throw new ArgumentOutOfRangeException($"{nameof(size)} is invalid");
            }
            if(pixels.Length < size.X * size.Y) {
                ThrowPixelsTooShort();
                [DoesNotReturn] static void ThrowPixelsTooShort() => throw new ArgumentException($"{nameof(pixels)} is too short");
            }
            fixed(Color4* ptr = pixels) {
                LoadCore(size, ptr);
            }
        }

        public unsafe void Load(in Vector2i size, in ColorByte fill)
        {
            using var pixels = new UnsafeRawArray<ColorByte>(size.X * size.Y, false);
            pixels.AsSpan().Fill(fill);
            LoadCore(size, pixels.GetPtr());
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
            TextureObject.SubImage2D(rect, pixels);
            TextureObject.Unbind2D();
        }

        internal static unsafe void UpdateSubTexture(in TextureObject to, Color4* pixels, in RectI rect)
        {
            if(to.IsEmpty) {
                ThrowEmptyTexture();
            }
            TextureObject.Bind2D(to);
            TextureObject.SubImage2D(rect, pixels);
            TextureObject.Unbind2D();
        }

        public unsafe int GetPixels(in RectI rect, Span<ColorByte> buffer)
        {
            if(Texture.IsEmpty) {
                return 0;
            }
            if((uint)rect.X >= (uint)Size.X ||
               (uint)rect.Y >= (uint)Size.Y ||
               (uint)rect.Width > (uint)(Size.X - rect.X) ||
               (uint)rect.Height > (uint)(Size.Y - rect.Y)) {
                ThrowOutOfRange(nameof(rect));
            }
            var len = rect.X * rect.Y * sizeof(ColorByte);
            if(buffer.Length < len) {
                ThrowOutOfRange($"{nameof(buffer)} is too short.");
            }

            // Get current binded fbo
            var currentRead = FBO.CurrentReadBinded;
            var currentDraw = FBO.CurrentDrawBinded;

            var fbo = FBO.Create();
            try {
                FBO.Bind(fbo, FBO.Target.FrameBuffer);
                FBO.SetTexture2DBuffer(Texture, FBO.Attachment.ColorAttachment0);
                if(!FBO.CheckStatus(out var error)) {
                    ThrowInvalidFBO(error);
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

            [DoesNotReturn] static void ThrowOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);
            [DoesNotReturn] static void ThrowInvalidFBO(string message) => throw new InvalidOperationException(message);
        }

        internal static unsafe int GetPixels(in TextureObject to, in RectI rect, Span<ColorByte> buffer)
        {
            // I don't check that rect is valid here.
            if(to.IsEmpty) {
                return 0;
            }
            var len = rect.X * rect.Y * sizeof(ColorByte);
            if(buffer.Length < len) {
                ThrowOutOfRange($"{nameof(buffer)} is too short.");
            }

            // Get current binded fbo
            var currentRead = FBO.CurrentReadBinded;
            var currentDraw = FBO.CurrentDrawBinded;

            var fbo = FBO.Create();
            try {
                FBO.Bind(fbo, FBO.Target.FrameBuffer);
                FBO.SetTexture2DBuffer(to, FBO.Attachment.ColorAttachment0);
                if(!FBO.CheckStatus(out var error)) {
                    ThrowInvalidFBO(error);
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

            [DoesNotReturn] static void ThrowOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);
            [DoesNotReturn] static void ThrowInvalidFBO(string message) => throw new InvalidOperationException(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TexturePainter GetPainter(bool copyFromOriginal = true)
        {
            if(Texture.IsEmpty) {
                ThrowEmptyTexture();
            }

            return new TexturePainter(Texture, new RectI(default, Size), copyFromOriginal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TexturePainter GetPainter(in RectI rect, bool copyFromOriginal = true)
        {
            if(Texture.IsEmpty) {
                ThrowEmptyTexture();
            }

            if((uint)rect.X >= (uint)Size.X ||
               (uint)rect.Y >= (uint)Size.Y ||
               (uint)rect.Width > (uint)(Size.X - rect.X) ||
               (uint)rect.Height > (uint)(Size.Y - rect.Y)) {
                ThrowOutOfRange();

                [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(rect));
            }

            return new TexturePainter(Texture, rect, copyFromOriginal);
        }

        public void Dispose()
        {
            TextureObject.Delete(ref Texture);
            Size = default;
        }

        private unsafe void LoadCore<TColor>(in Vector2i size, TColor* pixels) where TColor : unmanaged
        {
            if(typeof(TColor) != typeof(ColorByte) && typeof(TColor) != typeof(Color4)) {
                ThrowNotSupported();
                [DoesNotReturn] static void ThrowNotSupported() => throw new NotSupportedException();
            }

            Texture = TextureObject.Create();
            TextureObject.Bind2D(Texture);
            TextureObject.Parameter2DMinFilter(ShrinkMode, MipmapMode);
            TextureObject.Parameter2DMagFilter(ExpansionMode);
            TextureObject.Parameter2DWrapS(WrapModeX);
            TextureObject.Parameter2DWrapT(WrapModeY);

            if(typeof(TColor) == typeof(ColorByte)) {
                TextureObject.Image2D(size, (ColorByte*)pixels);
            }
            else if(typeof(TColor) == typeof(Color4)) {
                TextureObject.Image2D(size, (Color4*)pixels);
            }

            if(MipmapMode != TextureMipmapMode.None) {
                TextureObject.GenerateMipmap2D();
            }
            TextureObject.Unbind2D();
            Size = size;
        }

        [DoesNotReturn]
        static void ThrowAlreadyLoaded() => throw new InvalidOperationException("Texture is already loaded.");

        [DoesNotReturn]
        static void ThrowEmptyTexture() => throw new InvalidOperationException("Texture is empty");
    }
}
