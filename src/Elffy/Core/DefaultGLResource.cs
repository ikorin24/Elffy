#nullable enable
using Elffy.Exceptions;
using Elffy.OpenGL;
using System;

namespace Elffy.Core
{
    internal sealed class DefaultGLResource : IDefaultResource, IDisposable
    {
        private bool _disposed;
        private TextureObject _whiteEmptyTexture;

        public TextureObject WhiteEmptyTexture => _whiteEmptyTexture;

        public DefaultGLResource()
        {
        }

        ~DefaultGLResource() => Dispose(false);

        public unsafe void Init()
        {
            var whiteEmpty = TextureObject.Create();
            var pixel = ColorByte.White;
            const TextureUnitNumber unit = TextureUnitNumber.Unit0;
            TextureObject.Bind2D(whiteEmpty, unit);
            TextureObject.Image2D(new(1, 1), &pixel);
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

    public interface IDefaultResource
    {
        TextureObject WhiteEmptyTexture { get; }
    }
}
