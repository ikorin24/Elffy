#nullable enable
using Elffy.Core;
using Elffy.OpenGL;
using Elffy.Exceptions;
using Elffy.Effective;
using System;
using System.Runtime.CompilerServices;
using OpenToolkit.Graphics.OpenGL4;
using TKPixelFormat = OpenToolkit.Graphics.OpenGL4.PixelFormat;

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

        public void Update(ReadOnlySpan<Vector4> texels, int xOffset) => _impl.Update(texels.MarshalCast<Vector4, Color4>(), xOffset);
        
        public void Update(ReadOnlySpan<Color4> texels, int xOffset) => _impl.Update(texels, xOffset);

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

        void IComponent.OnAttached(ComponentOwner owner) => _core.OnAttached(owner);

        void IComponent.OnDetached(ComponentOwner owner) => _core.OnDetachedForDisposable(owner, this);
    }

    internal readonly struct FloatDataTextureImpl : IDisposable
    {
        public readonly TextureObject TextureObject;
        public readonly int Length;

        public void Apply(TextureUnitNumber unit)
        {
            TextureObject.Bind1D(TextureObject, unit);
        }

        public unsafe void Load(ReadOnlySpan<Color4> texels)
        {
            if(!TextureObject.IsEmpty) {
                throw new InvalidOperationException("Already loaded");
            }
            if(texels.IsEmpty) { return; }
            
            Unsafe.AsRef(TextureObject) = TextureObject.Create();
            Unsafe.AsRef(Length) = texels.Length;

            const TextureUnitNumber unit = TextureUnitNumber.Unit1;
            TextureObject.Bind1D(TextureObject, unit);
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            fixed(void* ptr = texels) {
                GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgba32f,
                              texels.Length, 0, TKPixelFormat.Rgba, PixelType.Float, (IntPtr)ptr);
            }
            TextureObject.Unbind1D(unit);
        }

        public unsafe void Update(ReadOnlySpan<Color4> texels, int xOffset)
        {
            if(TextureObject.IsEmpty) { throw new InvalidOperationException("Cannnot update texels because not loaded yet."); }
            if((uint)xOffset >= (uint)Length || texels.Length > Length - xOffset) {
                throw new ArgumentOutOfRangeException($"Length: {Length}, {nameof(texels)}.Length: {texels.Length}, {nameof(xOffset)}: {xOffset}");
            }

            if(texels.IsEmpty) { return; }

            const TextureUnitNumber unit = TextureUnitNumber.Unit1;
            TextureObject.Bind1D(TextureObject, unit);
            fixed(void* ptr = texels) {
                GL.TexSubImage1D(TextureTarget.Texture1D, 0, xOffset,
                                 texels.Length, TKPixelFormat.Rgba, PixelType.Float, (IntPtr)ptr);
            }
            TextureObject.Unbind1D(unit);
        }

        public void Dispose()
        {
            TextureObject.Delete(ref Unsafe.AsRef(TextureObject));
            Unsafe.AsRef(Length) = 0;
        }
    }
}
