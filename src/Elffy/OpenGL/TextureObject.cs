#nullable enable
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL4;
using Elffy.Core;
using Elffy.Imaging;
using TKPixelType = OpenTK.Graphics.OpenGL4.PixelType;
using TKPixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace Elffy.OpenGL
{
    [DebuggerDisplay("Texture={_texture}")]
    public readonly struct TextureObject : IEquatable<TextureObject>
    {
        private readonly int _texture;

        internal int Value => _texture;

        public bool IsEmpty => _texture == Consts.NULL;

        public static TextureObject Empty => new TextureObject();

        private TextureObject(int texture)
        {
            _texture = texture;
        }

        internal static TextureObject Create()
        {
            GLAssert.EnsureContext();
            return new TextureObject(GL.GenTexture());
        }

        internal static void Bind2D(in TextureObject to, TextureUnitNumber textureUnit)
        {
            var glTextureUnit = (TextureUnit)((int)TextureUnit.Texture0 + textureUnit);

            GLAssert.EnsureContext();
            GL.ActiveTexture(glTextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, to._texture);
        }

        internal static void Bind1D(in TextureObject to, TextureUnitNumber textureUnit)
        {
            var glTextureUnit = (TextureUnit)((int)TextureUnit.Texture0 + textureUnit);

            GLAssert.EnsureContext();
            GL.ActiveTexture(glTextureUnit);
            GL.BindTexture(TextureTarget.Texture1D, to._texture);
        }

        internal static void Unbind2D(TextureUnitNumber textureUnit)
        {
            Bind2D(Empty, textureUnit);
        }

        internal static void Unbind1D(TextureUnitNumber textureUnit)
        {
            Bind1D(Empty, textureUnit);
        }

        internal static unsafe void Image2D(Bitmap bitmap)
        {
            using(var pixels = bitmap.GetPixels(ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, pixels.Width, pixels.Height,
                              0, TKPixelFormat.Bgra, TKPixelType.UnsignedByte, pixels.Ptr);
            }
        }

        internal static unsafe void Image2D(in Vector2i size, ColorByte* pixels)
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, size.X, size.Y,
                          0, TKPixelFormat.Rgba, TKPixelType.UnsignedByte, (IntPtr)pixels);
        }

        internal static unsafe void Image2D(in Vector2i size, Color4* pixels)
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, size.X, size.Y,
                          0, TKPixelFormat.Rgba, TKPixelType.Float, (IntPtr)pixels);
        }

        internal static unsafe void SubImage2D(in RectI rect, ColorByte* pixels)
        {
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, rect.X, rect.Y, rect.Width, rect.Height,
                             TKPixelFormat.Rgba, TKPixelType.UnsignedByte, (IntPtr)pixels);
        }

        internal static unsafe void SubImage2D(in RectI rect, Color4* pixels)
        {
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, rect.X, rect.Y, rect.Width, rect.Height,
                             TKPixelFormat.Rgba, TKPixelType.Float, (IntPtr)pixels);
        }

        internal static void Parameter2DMinFilter(TextureShrinkMode shrinkMode,
                                                  TextureMipmapMode mipmapMode = TextureMipmapMode.None)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, GetMinParameter(shrinkMode, mipmapMode));
        }

        internal static void Parameter2DMagFilter(TextureExpansionMode expansionMode)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, GetMagParameter(expansionMode));
        }

        internal static void GenerateMipmap2D()
        {
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        internal static unsafe void Image1D(int width, ColorByte* pixels)
        {
            GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgba, width,
                          0, TKPixelFormat.Rgba, TKPixelType.UnsignedByte, (IntPtr)pixels);
        }

        internal static unsafe void Image1D(int width, Color4* pixels)
        {
            GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgba, width,
                          0, TKPixelFormat.Rgba, TKPixelType.Float, (IntPtr)pixels);
        }

        internal static unsafe void Image1DAsRgba32f(int width, Color4* pixels)
        {
            GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgba32f,
                          width, 0, TKPixelFormat.Rgba, TKPixelType.Float, (IntPtr)pixels);
        }

        internal static unsafe void SubImage1D(int xOffset, int width, ColorByte* pixels)
        {
            GL.TexSubImage1D(TextureTarget.Texture1D, 0, xOffset, width,
                             TKPixelFormat.Rgba, TKPixelType.UnsignedByte, (IntPtr)pixels);
        }

        internal static unsafe void SubImage1D(int xOffset, int width, Color4* pixels)
        {
            GL.TexSubImage1D(TextureTarget.Texture1D, 0, xOffset, width,
                             TKPixelFormat.Rgba, TKPixelType.Float, (IntPtr)pixels);
        }

        internal static void Parameter1DMinFilter(TextureShrinkMode shrinkMode,
                                                  TextureMipmapMode mipmapMode = TextureMipmapMode.None)
        {
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMinFilter, GetMinParameter(shrinkMode, mipmapMode));
        }

        internal static void Parameter1DMagFilter(TextureExpansionMode expansionMode)
        {
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMagFilter, GetMagParameter(expansionMode));
        }

        internal static void Delete(ref TextureObject to)
        {
            if(to._texture != Consts.NULL) {
                GLAssert.EnsureContext();
                GL.DeleteTexture(to._texture);
                to = default;
            }
        }

        private static int GetMagParameter(TextureExpansionMode expansionMode)
        {
            switch(expansionMode) {
                case TextureExpansionMode.Bilinear:
                    return (int)TextureMagFilter.Linear;
                case TextureExpansionMode.NearestNeighbor:
                    return (int)TextureMagFilter.Nearest;
                default:
                    throw new ArgumentException();
            }
        }

        private static int GetMinParameter(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode)
        {
            switch(shrinkMode) {
                case TextureShrinkMode.Bilinear:
                    switch(mipmapMode) {
                        case TextureMipmapMode.None:
                            return (int)TextureMinFilter.Linear;
                        case TextureMipmapMode.Bilinear:
                            return (int)TextureMinFilter.LinearMipmapLinear;
                        case TextureMipmapMode.NearestNeighbor:
                            return (int)TextureMinFilter.LinearMipmapNearest;
                    }
                    break;
                case TextureShrinkMode.NearestNeighbor:
                    switch(mipmapMode) {
                        case TextureMipmapMode.None:
                            return (int)TextureMinFilter.Nearest;
                        case TextureMipmapMode.Bilinear:
                            return (int)TextureMinFilter.NearestMipmapLinear;
                        case TextureMipmapMode.NearestNeighbor:
                            return (int)TextureMinFilter.NearestMipmapNearest;
                    }
                    break;
            }
            throw new ArgumentException();
        }

        public override string ToString() => _texture.ToString();

        public override bool Equals(object? obj) => obj is TextureObject to && Equals(to);

        public bool Equals(TextureObject other) => _texture == other._texture;

        public override int GetHashCode() => _texture.GetHashCode();
    }
}
