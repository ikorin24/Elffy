﻿#nullable enable
using Elffy.Imaging;
using Elffy.Graphics.OpenGL;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Features
{
    public static class TextureLoadHelper
    {
        public static unsafe TextureObject LoadByDMA(in ReadOnlyImageRef image, TextureExpansionMode expansionMode,
                                                     TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode,
                                                     TextureWrap wrapModeX, TextureWrap wrapModeY)
        {
            fixed(ColorByte* ptr = image) {
                var state = (Ptr: (IntPtr)ptr, Length: image.Width * image.Height);
                return LoadByDMA(
                    state,
                    new Vector2i(image.Width, image.Height),
                    static (state, dest) => new Span<ColorByte>((void*)state.Ptr, state.Length).CopyTo(dest.GetPixels()),
                    expansionMode, shrinkMode, mipmapMode, wrapModeX, wrapModeY);
            }
        }

        public static unsafe TextureObject LoadByDMA<T>
            (T state, in Vector2i size, ImageBuilderDelegate<T> imageBuilder,
             TextureExpansionMode expansionMode, TextureShrinkMode shrinkMode,
             TextureMipmapMode mipmapMode, TextureWrap wrapModeX, TextureWrap wrapModeY)
        {
            if(size.X <= 0 || size.Y <= 0) {
                ThrowInvalidSize($"{nameof(size)} is invalid");
            }
            if(imageBuilder is null) {
                ThrowNullArg(nameof(imageBuilder));
            }
            var pbo = PBO.Create();
            PBO.Bind(pbo, BufferPackTarget.PixelUnpackBuffer);
            try {
                PBO.BufferData(BufferPackTarget.PixelUnpackBuffer, size.X * size.Y * sizeof(ColorByte), IntPtr.Zero, BufferHint.StaticDraw);
                var pixels = PBO.MapBuffer<ColorByte>(BufferPackTarget.PixelUnpackBuffer, BufferAccessMode.WriteOnly);
                try {
                    var dest = new ImageRef(pixels, size.X, size.Y);
                    imageBuilder.Invoke(state, dest);
                }
                finally {
                    PBO.UnmapBuffer(BufferPackTarget.PixelUnpackBuffer);
                }
                var tex = TextureObject.Create();
                TextureObject.Bind2D(tex);
                try {
                    TextureObject.Parameter2DMinFilter(shrinkMode, mipmapMode);
                    TextureObject.Parameter2DMagFilter(expansionMode);
                    TextureObject.Parameter2DWrapS(wrapModeX);
                    TextureObject.Parameter2DWrapT(wrapModeY);
                    TextureObject.Image2D(size, (ColorByte*)null, 0);      // Allocate texture in VRAM and copy pixels data
                    if(mipmapMode != TextureMipmapMode.None) {
                        TextureObject.GenerateMipmap2D();
                    }
                }
                catch {
                    TextureObject.Delete(ref tex);
                    throw;
                }
                finally {
                    TextureObject.Unbind2D();
                }
                return tex;
            }
            finally {
                PBO.Unbind(BufferPackTarget.PixelUnpackBuffer);
                PBO.Delete(ref pbo);
            }
        }

        [DoesNotReturn]
        private static void ThrowInvalidSize(string message) => throw new ArgumentOutOfRangeException(message);

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);
    }
}
