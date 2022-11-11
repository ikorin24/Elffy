#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;
using Elffy.Imaging;

namespace Elffy.Graphics.OpenGL
{
    [DebuggerDisplay("{DebuggerView,nq}")]
    public readonly struct TextureObject : IEquatable<TextureObject>
    {
        private readonly int _texture;

        internal int Value => _texture;

        public bool IsEmpty => _texture == 0;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerView => IsEmpty ? "Texture (null)" : $"Texture={_texture}";

        /// <summary>Get empty <see cref="TextureObject"/></summary>
        public static TextureObject Empty => new TextureObject();

        private TextureObject(int texture)
        {
            _texture = texture;
        }

        /// <summary>Create texture object (Call glGenTexture)</summary>
        /// <returns>texture object</returns>
        public static TextureObject Create()
        {
            GLAssert.EnsureContext();
            return new TextureObject(GL.GenTexture());
        }

        /// <summary>Get available max texture unit count of fragment shader. (Call glGetIntegerv with GL_MAX_TEXTURE_IMAGE_UNITS)</summary>
        /// <returns>max texture unit count</returns>
        public static int GetFragmentShaderMaxTextureUnitCount()
        {
            GL.GetInteger(GetPName.MaxTextureImageUnits, out var count);
            return count;
        }

        /// <summary>Get available max texture unit count of vertex shader. (Call glGetIntegerv with GL_MAX_VERTEX_TEXTURE_IMAGE_UNITS)</summary>
        /// <returns>max texture unit count</returns>
        public static int GetVertexShaderMaxTextureUnitCount()
        {
            GL.GetInteger(GetPName.MaxVertexTextureImageUnits, out var count);
            return count;
        }

        /// <summary>Call glBindTexture with texture2D</summary>
        /// <param name="to">texture object</param>
        public static void Bind2D(in TextureObject to)
        {
            GLAssert.EnsureContext();
            GL.BindTexture(TextureTarget.Texture2D, to._texture);
        }

        /// <summary>Call glBindTexture with texture1D</summary>
        /// <param name="to">texture object</param>
        public static void Bind1D(in TextureObject to)
        {
            GLAssert.EnsureContext();
            GL.BindTexture(TextureTarget.Texture1D, to._texture);
        }

        public static void Bind2DArray(in TextureObject to)
        {
            GLAssert.EnsureContext();
            GL.BindTexture(TextureTarget.Texture2DArray, to._texture);
        }

        /// <summary>Call glBindTexture with texture2D as 0</summary>
        public static void Unbind2D()
        {
            Bind2D(Empty);
        }

        /// <summary>Call glBindTexture with texture1d as 0</summary>
        public static void Unbind1D()
        {
            Bind1D(Empty);
        }

        public static void Unbind2DArray()
        {
            Bind2DArray(Empty);
        }

        public static unsafe void Image2DArray(in Vector2i size, int depth, ColorByte* pixels, int level)
        {
            GL.TexImage3D(TextureTarget.Texture2DArray, level, PixelInternalFormat.Rgba, size.X, size.Y, depth,
                          0, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)pixels);
        }

        public static unsafe void GetImage2D(Color4* pixels)
        {
            GetImage2D(pixels, 0);
        }

        public static unsafe void GetImage2D(ColorByte* pixels)
        {
            GetImage2D(pixels, 0);
        }

        public static unsafe void GetImage2D(Color4* pixels, int level)
        {
            GL.GetTexImage(TextureTarget.Texture2D, level, PixelFormat.Rgba, PixelType.Float, (IntPtr)pixels);
        }

        public static unsafe void GetImage2D(ColorByte* pixels, int level)
        {
            GL.GetTexImage(TextureTarget.Texture2D, level, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)pixels);
        }

        public static unsafe void GetImage1D(Color4* pixels)
        {
            GetImage1D(pixels, 0);
        }

        public static unsafe void GetImage1D(ColorByte* pixels)
        {
            GetImage1D(pixels, 0);
        }

        public static unsafe void GetImage1D(Color4* pixels, int level)
        {
            GL.GetTexImage(TextureTarget.Texture1D, level, PixelFormat.Rgba, PixelType.Float, (IntPtr)pixels);
        }

        public static unsafe void GetImage1D(ColorByte* pixels, int level)
        {
            GL.GetTexImage(TextureTarget.Texture1D, level, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)pixels);
        }

        /// <summary>Call glTexImge2D</summary>
        /// <param name="image">image to load</param>
        /// <param name="level">texture level</param>
        public static unsafe void Image2D(in ReadOnlyImageRef image, int level)
        {
            fixed(ColorByte* pixels = image) {
                GL.TexImage2D(TextureTarget.Texture2D, level, PixelInternalFormat.Rgba, image.Width, image.Height,
                              0, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)pixels);
            }
        }

        /// <summary>Call glTexImage2D</summary>
        /// <remarks>Allocate memory of specified <paramref name="size"/> without initialization when set <see langword="null"/> to <paramref name="pixels"/>.</remarks>
        /// <param name="size">texture size</param>
        /// <param name="pixels">texture to load</param>
        /// <param name="level">texture level</param>
        public static unsafe void Image2D(in Vector2i size, ColorByte* pixels, int level)
        {
            // Allocate memory of specified size without initialization
            // if pixels == null.

            GL.TexImage2D(TextureTarget.Texture2D, level, PixelInternalFormat.Rgba, size.X, size.Y,
                          0, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)pixels);
        }

        /// <summary>Call glTexImage2D</summary>
        /// <remarks>Allocate memory of specified <paramref name="size"/> without initialization when set <see langword="null"/> to <paramref name="pixels"/>.</remarks>
        /// <param name="size">texture size</param>
        /// <param name="pixels">texture to load</param>
        /// <param name="level">texture level</param>
        public static unsafe void Image2D(in Vector2i size, Color4* pixels, int level)
        {
            // Allocate memory of specified size without initialization
            // if pixels == null.

            GL.TexImage2D(TextureTarget.Texture2D, level, PixelInternalFormat.Rgba, size.X, size.Y,
                          0, PixelFormat.Rgba, PixelType.Float, (IntPtr)pixels);
        }

        /// <summary>Call glTexImage2D</summary>
        /// <remarks>Allocate memory of specified <paramref name="size"/> without initialization when set <see langword="null"/> to <paramref name="pixels"/>.</remarks>
        /// <param name="size">texture size</param>
        /// <param name="pixels">texture to load</param>
        /// <param name="internalFormat">internal format</param>
        /// <param name="level">texture level</param>
        public static unsafe void Image2D(in Vector2i size, ColorByte* pixels, TextureInternalFormat internalFormat, int level)
        {
            // Allocate memory of specified size without initialization
            // if pixels == null.

            GL.TexImage2D(TextureTarget.Texture2D, level, internalFormat.ToOriginalValue(), size.X, size.Y,
                          0, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)pixels);
        }

        /// <summary>Call glTexImage2D</summary>
        /// <remarks>Allocate memory of specified <paramref name="size"/> without initialization when set <see langword="null"/> to <paramref name="pixels"/>.</remarks>
        /// <param name="size">texture size</param>
        /// <param name="pixels">texture to load</param>
        /// <param name="internalFormat">internal format</param>
        /// <param name="level">texture level</param>
        public static unsafe void Image2D(in Vector2i size, Color4* pixels, TextureInternalFormat internalFormat, int level)
        {
            // Allocate memory of specified size without initialization
            // if pixels == null.

            GL.TexImage2D(TextureTarget.Texture2D, level, internalFormat.ToOriginalValue(), size.X, size.Y,
                          0, PixelFormat.Rgba, PixelType.Float, (IntPtr)pixels);
        }

        /// <summary>call glTexImage2D</summary>
        /// <param name="size">texture size</param>
        /// <param name="bits">depth texture bits (should be 16, 24 or 32)</param>
        public static unsafe void DepthImage2DUninitialized(in Vector2i size, int bits = 24)
        {
            // Allocate memory of specified size without initialization
            var internalFormat = bits switch
            {
                16 => PixelInternalFormat.DepthComponent16,
                24 => PixelInternalFormat.DepthComponent24,
                32 => PixelInternalFormat.DepthComponent32,
                _ => PixelInternalFormat.DepthComponent,    // OpenGL will determine the appropriate precision, but we do not know the value.
            };
            GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, size.X, size.Y,
                          0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        }

        /// <summary>call glTexImage3D with GL_TEXTURE_2D_ARRAY</summary>
        /// <param name="size">texture size</param>
        /// <param name="arrayLength">array length</param>
        /// <param name="bits">depth texture bits (should be 16, 24 or 32)</param>
        public static unsafe void DepthImage2DArrayUninitialized(in Vector2i size, int arrayLength, int bits = 24)
        {
            // Allocate memory of specified size without initialization
            var internalFormat = bits switch
            {
                16 => PixelInternalFormat.DepthComponent16,
                24 => PixelInternalFormat.DepthComponent24,
                32 => PixelInternalFormat.DepthComponent32,
                _ => PixelInternalFormat.DepthComponent,    // OpenGL will determine the appropriate precision, but we do not know the value.
            };
            GL.TexImage3D(TextureTarget.Texture2DArray, 0, internalFormat, size.X, size.Y, arrayLength,
                          0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        }

        /// <summary>Call glTexSubImage2D</summary>
        /// <param name="rect">sub texture rect</param>
        /// <param name="pixels">sub texture to load</param>
        /// <param name="level">texture level</param>
        public static unsafe void SubImage2D(in RectI rect, ColorByte* pixels, int level)
        {
            GL.TexSubImage2D(TextureTarget.Texture2D, level, rect.X, rect.Y, rect.Width, rect.Height,
                             PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)pixels);
        }

        /// <summary>Call glTexSubImage2D</summary>
        /// <param name="rect">sub texture rect</param>
        /// <param name="pixels">sub texture to load</param>
        /// <param name="level">texture level</param>
        public static unsafe void SubImage2D(in RectI rect, Color4* pixels, int level)
        {
            GL.TexSubImage2D(TextureTarget.Texture2D, level, rect.X, rect.Y, rect.Width, rect.Height,
                             PixelFormat.Rgba, PixelType.Float, (IntPtr)pixels);
        }

        /// <summary>Call glTexParameter with texture2D and texture min filter</summary>
        /// <param name="shrinkMode">shrink mode</param>
        /// <param name="mipmapMode">mipmap mode</param>
        public static void Parameter2DMinFilter(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, GetMinParameter(shrinkMode, mipmapMode));
        }

        /// <summary>Call glTexParameter with texture2D and texture mag filter</summary>
        /// <param name="expansionMode">expantion mode</param>
        public static void Parameter2DMagFilter(TextureExpansionMode expansionMode)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, GetMagParameter(expansionMode));
        }

        /// <summary>Call glTexparameter with texture2D and texture wrap s</summary>
        /// <param name="wrapMode">texture wrap mode</param>
        public static void Parameter2DWrapS(TextureWrap wrapMode)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, GetWrapMode(wrapMode));
        }

        /// <summary>Call glTexparameter with texture2D and texture wrap t</summary>
        /// <param name="wrapMode">texture wrap mode</param>
        public static void Parameter2DWrapT(TextureWrap wrapMode)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, GetWrapMode(wrapMode));
        }

        /// <summary>Call glGenerateMipmap with texture2D</summary>
        public static void GenerateMipmap2D()
        {
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        /// <summary>Call glTexParameter with texture2Darray and texture min filter</summary>
        /// <param name="shrinkMode">shrink mode</param>
        /// <param name="mipmapMode">mipmap mode</param>
        public static void Parameter2DArrayMinFilter(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode)
        {
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, GetMinParameter(shrinkMode, mipmapMode));
        }

        /// <summary>Call glTexParameter with texture2Darray and texture mag filter</summary>
        /// <param name="expansionMode">expantion mode</param>
        public static void Parameter2DArrayMagFilter(TextureExpansionMode expansionMode)
        {
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, GetMagParameter(expansionMode));
        }

        /// <summary>Call glTexparameter with texture2Darray and texture wrap s</summary>
        /// <param name="wrapMode">texture wrap mode</param>
        public static void Parameter2DArrayWrapS(TextureWrap wrapMode)
        {
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, GetWrapMode(wrapMode));
        }

        /// <summary>Call glTexparameter with texture2Darray and texture wrap t</summary>
        /// <param name="wrapMode">texture wrap mode</param>
        public static void Parameter2DArrayWrapT(TextureWrap wrapMode)
        {
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, GetWrapMode(wrapMode));
        }

        /// <summary>Call glGenerateMipmap with texture2D</summary>
        public static void GenerateMipmap2DArray()
        {
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
        }

        /// <summary>Call glTexImage1D</summary>
        /// <param name="width">texture width</param>
        /// <param name="pixels">texture to load</param>
        /// <param name="level">texture level</param>
        public static unsafe void Image1D(int width, ColorByte* pixels, int level)
        {
            GL.TexImage1D(TextureTarget.Texture1D, level, PixelInternalFormat.Rgba, width,
                          0, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)pixels);
        }

        /// <summary>Call glTexImage1D</summary>
        /// <param name="width">texture width</param>
        /// <param name="pixels">texture to load</param>
        /// <param name="level">texture level</param>
        public static unsafe void Image1D(int width, Color4* pixels, int level)
        {
            GL.TexImage1D(TextureTarget.Texture1D, level, PixelInternalFormat.Rgba, width,
                          0, PixelFormat.Rgba, PixelType.Float, (IntPtr)pixels);
        }

        /// <summary>Call glTexImage1D</summary>
        /// <param name="width">texture width</param>
        /// <param name="pixels">texture to load</param>
        /// <param name="internalFormat">internal format</param>
        /// <param name="level">texture level</param>
        public static unsafe void Image1D(int width, ColorByte* pixels, TextureInternalFormat internalFormat, int level)
        {
            GL.TexImage1D(TextureTarget.Texture1D, level, internalFormat.ToOriginalValue(), width,
                          0, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)pixels);
        }

        /// <summary>Call glTexImage1D</summary>
        /// <param name="width">texture width</param>
        /// <param name="pixels">texture to load</param>
        /// <param name="internalFormat">internal format</param>
        /// <param name="level">texture level</param>
        public static unsafe void Image1D(int width, Color4* pixels, TextureInternalFormat internalFormat, int level)
        {
            GL.TexImage1D(TextureTarget.Texture1D, level, internalFormat.ToOriginalValue(), width,
                          0, PixelFormat.Rgba, PixelType.Float, (IntPtr)pixels);
        }

        /// <summary>Call glTexSubImage1D</summary>
        /// <param name="xOffset">offset of sub texture</param>
        /// <param name="width">width of sub texture</param>
        /// <param name="pixels">sub texture to load</param>
        /// <param name="level">texture level</param>
        public static unsafe void SubImage1D(int xOffset, int width, ColorByte* pixels, int level)
        {
            GL.TexSubImage1D(TextureTarget.Texture1D, level, xOffset, width,
                             PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)pixels);
        }

        /// <summary>Call glTexSubImage1D</summary>
        /// <param name="xOffset">offset of sub texture</param>
        /// <param name="width">width of sub texture</param>
        /// <param name="pixels">sub texture to load</param>
        /// <param name="level">texture level</param>
        public static unsafe void SubImage1D(int xOffset, int width, Color4* pixels, int level)
        {
            GL.TexSubImage1D(TextureTarget.Texture1D, level, xOffset, width,
                             PixelFormat.Rgba, PixelType.Float, (IntPtr)pixels);
        }

        /// <summary>Call glTexParameter with texture1D and min filter</summary>
        /// <param name="shrinkMode">shrink mode</param>
        /// <param name="mipmapMode">mipmap mode</param>
        public static void Parameter1DMinFilter(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode)
        {
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMinFilter, GetMinParameter(shrinkMode, mipmapMode));
        }

        /// <summary>Call glTexparameter with texture1D and mag filter</summary>
        /// <param name="expansionMode">expansion mode</param>
        public static void Parameter1DMagFilter(TextureExpansionMode expansionMode)
        {
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMagFilter, GetMagParameter(expansionMode));
        }

        /// <summary>Call glTexparameter with texture1D and texture wrap s</summary>
        /// <param name="wrapMode">texture wrap mode</param>
        public static void Parameter1DWrapS(TextureWrap wrapMode)
        {
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureWrapS, GetWrapMode(wrapMode));
        }

        /// <summary>Call glGenerateMipmap with texture1D</summary>
        public static void GenerateMipmap1D()
        {
            GL.GenerateMipmap(GenerateMipmapTarget.Texture1D);
        }

        /// <summary>Call glDeleteTexture</summary>
        /// <param name="to">texture object to delete</param>
        public static void Delete(ref TextureObject to)
        {
            if(to._texture != 0) {
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

        private static int GetWrapMode(TextureWrap wrapMode)
        {
            return (int)(wrapMode switch
            {
                TextureWrap.Repeat => TextureWrapMode.Repeat,
                TextureWrap.MirroredRepeat => TextureWrapMode.MirroredRepeat,
                TextureWrap.ClampToBorder => TextureWrapMode.ClampToBorder,
                TextureWrap.ClampToEdge => TextureWrapMode.ClampToEdge,
                _ => ThrowInvalidEnum(),
            });

            [DoesNotReturn] static TextureWrapMode ThrowInvalidEnum() => throw new ArgumentException();
        }

        public override string ToString() => _texture.ToString();

        public override bool Equals(object? obj) => obj is TextureObject to && Equals(to);

        public bool Equals(TextureObject other) => _texture == other._texture;

        public override int GetHashCode() => _texture.GetHashCode();
    }
}
