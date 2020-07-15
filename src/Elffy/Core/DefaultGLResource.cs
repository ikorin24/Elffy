#nullable enable
using Elffy.Exceptions;
using Elffy.OpenGL;
using System;
using OpenToolkit.Graphics.OpenGL;
using TKPixelFormat = OpenToolkit.Graphics.OpenGL.PixelFormat;

namespace Elffy.Core
{
    internal sealed class DefaultGLResource : IDisposable
    {
        private bool _disposed;
        private TextureObject _whiteEmptyTexture;

        public TextureObject WhiteEmptyTexture => _whiteEmptyTexture;

        public DefaultGLResource()
        {
        }

        ~DefaultGLResource() => Dispose(false);

        public unsafe void Create()
        {
            var whiteEmpty = TextureObject.Create();
            var pixel = Color4.White;
            var unit = TextureUnitNumber.Unit0;
            TextureObject.Bind2D(whiteEmpty, unit);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1, 1, 0, TKPixelFormat.Rgba, PixelType.Float, new IntPtr(&pixel));
            _whiteEmptyTexture = whiteEmpty;
            TextureObject.Unbind2D(unit);
        }

        public void Dispose()
        {
            if(_disposed) { return; }
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(disposing) {
                TextureObject.Delete(ref _whiteEmptyTexture);
            }
            else {
                throw new MemoryLeakException(typeof(DefaultGLResource));
            }
            _disposed = true;
        }
    }
}
