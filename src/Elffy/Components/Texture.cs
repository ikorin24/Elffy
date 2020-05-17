#nullable enable
using System;
using System.Drawing;
using System.Drawing.Imaging;
using Elffy.Core;
using Elffy.Exceptions;
using Elffy.Imaging;
using OpenTK.Graphics.OpenGL;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using TKPixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace Elffy.Components
{
    public sealed class Texture : IComponent, IDisposable
    {
        private int _textureBuffer;
        public TextureExpansionMode ExpansionMode { get; }
        public TextureShrinkMode ShrinkMode { get; }
        public TextureMipmapMode MipmapMode { get; }

        public Texture(TextureExpansionMode expansionMode, TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode)
        {
            ExpansionMode = expansionMode;
            ShrinkMode = shrinkMode;
            MipmapMode = mipmapMode;
        }

        ~Texture() => Dispose(false);

        public void Apply()
        {
            GL.BindTexture(TextureTarget.Texture2D, _textureBuffer);
        }

        public void Load(Bitmap bitmap)
        {
            if(bitmap is null) { throw new ArgumentNullException(nameof(bitmap)); }
            using(var pixels = bitmap.GetPixels(ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)) {
                Load(pixels.Width, pixels.Height, pixels.Ptr);
            }
        }

        private void Load(int pixelWidth, int pixelHeight, IntPtr ptr)
        {
            if(_textureBuffer != Consts.NULL) { throw new InvalidOperationException("Texture is already loaded."); }

            _textureBuffer = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _textureBuffer);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, GetMinParameter(ShrinkMode, MipmapMode));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, GetMagParameter(ExpansionMode));
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, pixelWidth, pixelHeight, 0, TKPixelFormat.Bgra, PixelType.UnsignedByte, ptr);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public void OnAttached(ComponentOwner owner) { }    // nop

        public void OnDetached(ComponentOwner owner) { }    // nop

        public void Dispose()
        {
            if(_textureBuffer == Consts.NULL) { return; }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(_textureBuffer == Consts.NULL) { return; }
            if(disposing) {
                GL.DeleteTexture(_textureBuffer);
                _textureBuffer = Consts.NULL;
            }
            else {
                // opengl のバッファは作成したスレッドでしか解放できないため
                // ファイナライザ経由では解放不可
                throw new MemoryLeakException(typeof(Texture));
            }
        }


        private int GetMagParameter(TextureExpansionMode expansionMode)
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
        private int GetMinParameter(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode)
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
    }
}
