#nullable enable
using Elffy.OpenGL;
using System;
using OpenToolkit.Graphics.OpenGL;
using Elffy.Exceptions;
using TKPixelFormat = OpenToolkit.Graphics.OpenGL.PixelFormat;
using Elffy.Components;

namespace Elffy.Core
{
    public sealed class FloatDataTexture : IComponent, IDisposable
    {
        private FloatDataTextureImpl _impl;
        public TextureUnitNumber TextureUnit { get; }

        public FloatDataTexture(TextureUnitNumber unit)
        {
            TextureUnit = unit;
        }
        public void Apply() => _impl.Apply(TextureUnit);

        ~FloatDataTexture() => Dispose(false);

        public unsafe void Load(ReadOnlySpan<Color4> texels) => _impl.Load(texels);

        public void Dispose()
        {
            if(_impl.Disposed) { return; }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(disposing) {
                _impl.Dispose();
            }
            else {
                throw new MemoryLeakException(typeof(FloatDataTexture));     // GC スレッドからでは解放できないので
            }
        }
    }

    internal struct FloatDataTextureImpl : IDisposable
    {
        public const TextureUnit TargetTextureUnit = TextureUnit.Texture1;

        public bool Disposed;
        public TextureObject TextureObject;

        public void Apply(TextureUnitNumber unit)
        {
            TextureObject.Bind(TextureObject, unit);
        }

        public unsafe void Load(ReadOnlySpan<Color4> texels)
        {
            if(texels.IsEmpty) { return; }
            TextureObject = TextureObject.Create();
            var unit = TextureUnitNumber.Unit0;
            TextureObject.Bind(TextureObject, unit);
            fixed(void* ptr = texels) {
                GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgba, texels.Length, 0, TKPixelFormat.Rgba, PixelType.Float, (IntPtr)ptr);
            }
            TextureObject.Unbind(unit);
        }

        public void Dispose()
        {
            if(Disposed) { return; }
            Disposed = true;
            TextureObject.Delete(ref TextureObject);
        }
    }
}
