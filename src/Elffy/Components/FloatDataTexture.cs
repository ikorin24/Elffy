#nullable enable
using Elffy.OpenGL;
using System;
using OpenToolkit.Graphics.OpenGL;
using Elffy.Exceptions;
using TKPixelFormat = OpenToolkit.Graphics.OpenGL.PixelFormat;
using Elffy.Effective;
using Elffy.Core;
using System.Runtime.CompilerServices;

namespace Elffy.Components
{
    public sealed class FloatDataTexture : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<FloatDataTexture> _core = new SingleOwnerComponentCore<FloatDataTexture>(true);
        private FloatDataTextureImpl _impl = new FloatDataTextureImpl();

        public ComponentOwner? Owner => _core.Owner;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        public FloatDataTexture()
        {
        }

        public void Apply(TextureUnitNumber textureUnit) => _impl.Apply(textureUnit);

        ~FloatDataTexture() => Dispose(false);

        public void Load(ReadOnlySpan<Vector4> texels) => _impl.Load(texels.MarshalCast<Vector4, Color4>());

        public void Load(ReadOnlySpan<Color4> texels) => _impl.Load(texels);

        public void Dispose()
        {
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

        public void OnAttached(ComponentOwner owner) => _core.OnAttached(owner);

        public void OnDetached(ComponentOwner owner) => _core.OnDetachedForDisposable(owner, this);
    }

    internal readonly struct FloatDataTextureImpl : IDisposable
    {
        public readonly TextureObject TextureObject;

        public void Apply(TextureUnitNumber unit)
        {
            TextureObject.Bind1D(TextureObject, unit);
        }

        public unsafe void Load(ReadOnlySpan<Color4> texels)
        {
            if(texels.IsEmpty) { return; }
            var unit = TextureUnitNumber.Unit1;
            Unsafe.AsRef(TextureObject) = TextureObject.Create();
            TextureObject.Bind1D(TextureObject, unit);
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            
            fixed(void* ptr = texels) {
                GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgba32f,
                              texels.Length, 0, TKPixelFormat.Rgba, PixelType.Float, (IntPtr)ptr);
            }
            TextureObject.Unbind1D(unit);
        }

        public void Dispose()
        {
            TextureObject.Delete(ref Unsafe.AsRef(TextureObject));
        }
    }
}
