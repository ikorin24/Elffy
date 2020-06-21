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
        public const TextureUnit TargetTextureUnit = FloatDataTextureImpl.TargetTextureUnit;

        private FloatDataTextureImpl _impl;

        public void Apply() => _impl.Apply();

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

        public void OnAttached(ComponentOwner owner)
        {
            // nop
        }

        public void OnDetached(ComponentOwner owner)
        {
            // nop
        }
    }

    internal struct FloatDataTextureImpl : IDisposable
    {
        public const TextureUnit TargetTextureUnit = TextureUnit.Texture1;

        public bool Disposed;
        public TextureObject TextureObject;

        public void Apply()
        {
            TextureObject.Bind(TextureObject, FloatDataTexture.TargetTextureUnit);
        }

        public unsafe void Load(ReadOnlySpan<Color4> texels)
        {
            if(texels.IsEmpty) { return; }
            TextureObject = TextureObject.Create();
            TextureObject.Bind(TextureObject, FloatDataTexture.TargetTextureUnit);
            fixed(void* ptr = texels) {
                GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgba, texels.Length, 0, TKPixelFormat.Rgba, PixelType.Float, (IntPtr)ptr);
            }
            TextureObject.Unbind(FloatDataTexture.TargetTextureUnit);
        }

        public void Dispose()
        {
            if(Disposed) { return; }
            Disposed = true;
            TextureObject.Delete(ref TextureObject);
        }
    }
}
