#nullable enable
using Elffy.OpenGL;
using System;
using OpenToolkit.Graphics.OpenGL;
using Elffy.Exceptions;
using TKPixelFormat = OpenToolkit.Graphics.OpenGL.PixelFormat;
using Elffy.Effective;
using Elffy.Core;

namespace Elffy.Components
{
    public sealed class FloatDataTexture : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<FloatDataTexture> _core = new SingleOwnerComponentCore<FloatDataTexture>(true);
        private FloatDataTextureImpl _impl;
        public TextureUnitNumber TextureUnit { get; }

        public ComponentOwner? Owner => _core.Owner;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        public FloatDataTexture(TextureUnitNumber unit)
        {
            TextureUnit = unit;
        }
        public void Apply() => _impl.Apply(TextureUnit);

        ~FloatDataTexture() => Dispose(false);

        public unsafe void Load(ReadOnlySpan<Vector4> texels) => _impl.Load(texels.MarshalCast<Vector4, Color4>());

        public unsafe void Load(ReadOnlySpan<Color4> texels) => _impl.Load(texels);

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

    internal struct FloatDataTextureImpl : IDisposable
    {
        public const TextureUnit TargetTextureUnit = TextureUnit.Texture1;

        public TextureObject TextureObject;

        public void Apply(TextureUnitNumber unit)
        {
            TextureObject.Bind1D(TextureObject, unit);
        }

        public unsafe void Load(ReadOnlySpan<Color4> texels)
        {
            if(texels.IsEmpty) { return; }
            var unit = TextureUnitNumber.Unit1;
            TextureObject = TextureObject.Create();
            TextureObject.Bind1D(TextureObject, unit);
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            fixed(void* ptr = texels) {

                // おそらくデータは 0 ~ 1 に丸められます。
                // OpenGL の内部挙動 (仕様？) を把握しきれてないが、実験してみたらおそらく丸められている。

                GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgba, texels.Length, 0, TKPixelFormat.Rgba, PixelType.Float, (IntPtr)ptr);
            }
            TextureObject.Unbind1D(unit);
        }

        public void Dispose()
        {
            TextureObject.Delete(ref TextureObject);
        }
    }
}
