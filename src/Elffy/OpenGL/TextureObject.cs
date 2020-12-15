#nullable enable
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;
using Elffy.Core;
using Elffy.Imaging;
using TKPixelType = OpenTK.Graphics.OpenGL4.PixelType;
using TKPixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using GLTextureWrapMode = OpenTK.Graphics.OpenGL4.TextureWrapMode;

namespace Elffy.OpenGL
{
    [DebuggerDisplay("Texture={_texture}")]
    public readonly struct TextureObject : IEquatable<TextureObject>
    {
        private readonly int _texture;

        internal int Value => _texture;

        public bool IsEmpty => _texture == Consts.NULL;

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

        /// <summary>Call glActivateTexture and glBindTexture</summary>
        /// <param name="to">texture object</param>
        /// <param name="textureUnit">texture unit</param>
        public static void Bind2D(in TextureObject to, TextureUnitNumber textureUnit = TextureUnitNumber.Unit0)
        {
            var glTextureUnit = (TextureUnit)((int)TextureUnit.Texture0 + textureUnit);

            GLAssert.EnsureContext();
            GL.ActiveTexture(glTextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, to._texture);
        }

        /// <summary>Call glActivateTexture adn glBindTexture</summary>
        /// <param name="to">texture object</param>
        /// <param name="textureUnit">texture unit</param>
        public static void Bind1D(in TextureObject to, TextureUnitNumber textureUnit = TextureUnitNumber.Unit0)
        {
            var glTextureUnit = (TextureUnit)((int)TextureUnit.Texture0 + textureUnit);

            GLAssert.EnsureContext();
            GL.ActiveTexture(glTextureUnit);
            GL.BindTexture(TextureTarget.Texture1D, to._texture);
        }

        /// <summary>Call glBindTexture(0)</summary>
        /// <param name="textureUnit"></param>
        public static void Unbind2D(TextureUnitNumber textureUnit = TextureUnitNumber.Unit0)
        {
            Bind2D(Empty, textureUnit);
        }

        /// <summary>Call glBindTexture(0)</summary>
        /// <param name="textureUnit"></param>
        public static void Unbind1D(TextureUnitNumber textureUnit = TextureUnitNumber.Unit0)
        {
            Bind1D(Empty, textureUnit);
        }

        /// <summary>Call glTexImage2D</summary>
        /// <param name="bitmap">texture to load</param>
        public static unsafe void Image2D(Bitmap bitmap)
        {
            using(var pixels = bitmap.GetPixels(ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, pixels.Width, pixels.Height,
                              0, TKPixelFormat.Bgra, TKPixelType.UnsignedByte, pixels.Ptr);
            }
        }

        /// <summary>Call glTexImage2D</summary>
        /// <param name="size">texture size</param>
        /// <param name="pixels">texture to load</param>
        public static unsafe void Image2D(in Vector2i size, ColorByte* pixels)
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, size.X, size.Y,
                          0, TKPixelFormat.Rgba, TKPixelType.UnsignedByte, (IntPtr)pixels);
        }

        /// <summary>Call glTexImage2D</summary>
        /// <param name="size">texture size</param>
        /// <param name="pixels">texture to load</param>
        public static unsafe void Image2D(in Vector2i size, Color4* pixels)
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, size.X, size.Y,
                          0, TKPixelFormat.Rgba, TKPixelType.Float, (IntPtr)pixels);
        }

        /// <summary>Call glTexImage2D</summary>
        /// <param name="size">texture size</param>
        /// <param name="pixels">texture to load</param>
        /// <param name="internalFormat">internal format</param>
        public static unsafe void Image2D(in Vector2i size, ColorByte* pixels, InternalFormat internalFormat)
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, (PixelInternalFormat)internalFormat, size.X, size.Y,
                          0, TKPixelFormat.Rgba, TKPixelType.UnsignedByte, (IntPtr)pixels);
        }

        /// <summary>Call glTexImage2D</summary>
        /// <param name="size">texture size</param>
        /// <param name="pixels">texture to load</param>
        /// <param name="internalFormat">internal format</param>
        public static unsafe void Image2D(in Vector2i size, Color4* pixels, InternalFormat internalFormat)
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, (PixelInternalFormat)internalFormat, size.X, size.Y,
                          0, TKPixelFormat.Rgba, TKPixelType.Float, (IntPtr)pixels);
        }

        /// <summary>Call glTexSubImage2D</summary>
        /// <param name="rect">sub texture rect</param>
        /// <param name="pixels">sub texture to load</param>
        public static unsafe void SubImage2D(in RectI rect, ColorByte* pixels)
        {
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, rect.X, rect.Y, rect.Width, rect.Height,
                             TKPixelFormat.Rgba, TKPixelType.UnsignedByte, (IntPtr)pixels);
        }

        /// <summary>Call glTexSubImage2D</summary>
        /// <param name="rect">sub texture rect</param>
        /// <param name="pixels">sub texture to load</param>
        public static unsafe void SubImage2D(in RectI rect, Color4* pixels)
        {
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, rect.X, rect.Y, rect.Width, rect.Height,
                             TKPixelFormat.Rgba, TKPixelType.Float, (IntPtr)pixels);
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
        public static void Parameter2DWrapS(TextureWrapMode wrapMode)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, GetWrapMode(wrapMode));
        }

        /// <summary>Call glTexparameter with texture2D and texture wrap t</summary>
        /// <param name="wrapMode">texture wrap mode</param>
        public static void Parameter2DWrapT(TextureWrapMode wrapMode)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, GetWrapMode(wrapMode));
        }

        /// <summary>Call glGenerateMipmap with texture2D</summary>
        public static void GenerateMipmap2D()
        {
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        /// <summary>Call glTexImage1D</summary>
        /// <param name="width">texture width</param>
        /// <param name="pixels">texture to load</param>
        public static unsafe void Image1D(int width, ColorByte* pixels)
        {
            GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgba, width,
                          0, TKPixelFormat.Rgba, TKPixelType.UnsignedByte, (IntPtr)pixels);
        }

        /// <summary>Call glTexImage1D</summary>
        /// <param name="width">texture width</param>
        /// <param name="pixels">texture to load</param>
        public static unsafe void Image1D(int width, Color4* pixels)
        {
            GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgba, width,
                          0, TKPixelFormat.Rgba, TKPixelType.Float, (IntPtr)pixels);
        }

        /// <summary>Call glTexImage1D</summary>
        /// <param name="width">texture width</param>
        /// <param name="pixels">texture to load</param>
        /// <param name="internalFormat">internal format</param>
        public static unsafe void Image1D(int width, ColorByte* pixels, InternalFormat internalFormat)
        {
            GL.TexImage1D(TextureTarget.Texture1D, 0, (PixelInternalFormat)internalFormat, width,
                          0, TKPixelFormat.Rgba, TKPixelType.UnsignedByte, (IntPtr)pixels);
        }

        /// <summary>Call glTexImage1D</summary>
        /// <param name="width">texture width</param>
        /// <param name="pixels">texture to load</param>
        /// <param name="internalFormat">internal format</param>
        public static unsafe void Image1D(int width, Color4* pixels, InternalFormat internalFormat)
        {
            GL.TexImage1D(TextureTarget.Texture1D, 0, (PixelInternalFormat)internalFormat, width,
                          0, TKPixelFormat.Rgba, TKPixelType.Float, (IntPtr)pixels);
        }

        /// <summary>Call glTexSubImage1D</summary>
        /// <param name="xOffset">offset of sub texture</param>
        /// <param name="width">width of sub texture</param>
        /// <param name="pixels">sub texture to load</param>
        public static unsafe void SubImage1D(int xOffset, int width, ColorByte* pixels)
        {
            GL.TexSubImage1D(TextureTarget.Texture1D, 0, xOffset, width,
                             TKPixelFormat.Rgba, TKPixelType.UnsignedByte, (IntPtr)pixels);
        }

        /// <summary>Call glTexSubImage1D</summary>
        /// <param name="xOffset">offset of sub texture</param>
        /// <param name="width">width of sub texture</param>
        /// <param name="pixels">sub texture to load</param>
        public static unsafe void SubImage1D(int xOffset, int width, Color4* pixels)
        {
            GL.TexSubImage1D(TextureTarget.Texture1D, 0, xOffset, width,
                             TKPixelFormat.Rgba, TKPixelType.Float, (IntPtr)pixels);
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
        public static void Parameter1DWrapS(TextureWrapMode wrapMode)
        {
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureWrapS, GetWrapMode(wrapMode));
        }

        /// <summary>Call glTexparameter with texture1D and texture wrap t</summary>
        /// <param name="wrapMode">texture wrap mode</param>
        public static void Parameter1DWrapT(TextureWrapMode wrapMode)
        {
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureWrapT, GetWrapMode(wrapMode));
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

        private static int GetWrapMode(TextureWrapMode wrapMode)
        {
            return (int)(wrapMode switch
            {
                TextureWrapMode.Repeat => GLTextureWrapMode.Repeat,
                TextureWrapMode.MirroredRepeat => GLTextureWrapMode.MirroredRepeat,
                TextureWrapMode.ClampToBorder => GLTextureWrapMode.ClampToBorder,
                TextureWrapMode.ClampToEdge => GLTextureWrapMode.ClampToEdge,
                _ => ThrowInvalidEnum(),
            });
            
            [DoesNotReturn] static GLTextureWrapMode ThrowInvalidEnum() => throw new ArgumentException();
        }

        public override string ToString() => _texture.ToString();

        public override bool Equals(object? obj) => obj is TextureObject to && Equals(to);

        public bool Equals(TextureObject other) => _texture == other._texture;

        public override int GetHashCode() => _texture.GetHashCode();


        public enum InternalFormat
        {
            /// <summary>(R8 G8 B8 A8), each channel is unsigned byte (0 ~ 255)</summary>
            Rgba8 = PixelInternalFormat.Rgba8,
            /// <summary>(R16 G16 B16 A16), each channel is 16bit floating point value</summary>
            Rgba16f = PixelInternalFormat.Rgba16f,
            /// <summary>(R32 G32 B32 A32), each channel is 32bit floating point value</summary>
            Rgba32f = PixelInternalFormat.Rgba32f,

            /// <summary>GL_DEPTH_COMPONENT. Use texture as a depth buffer. The driver chooses its precision.</summary>
            DepthComponent = PixelInternalFormat.DepthComponent,
            /// <summary>GL_DEPTH_COMPONENT16. Use texture as a depth buffer. 16 bits precision.</summary>
            DepthComponent16 = PixelInternalFormat.DepthComponent16,
            /// <summary>GL_DEPTH_COMPONENT24. Use texture as a depth buffer. 24 bits precision.</summary>
            DepthComponent24 = PixelInternalFormat.DepthComponent24,
        }
    }
}
