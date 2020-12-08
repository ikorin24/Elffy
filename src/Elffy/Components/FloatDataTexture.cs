#nullable enable
using Elffy.Core;
using Elffy.OpenGL;
using Elffy.Exceptions;
using Elffy.Effective;
using System;

namespace Elffy.Components
{
    public sealed class FloatDataTexture : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<FloatDataTexture> _core = new SingleOwnerComponentCore<FloatDataTexture>(true);    // Mutable object, Don't change into readonly
        private FloatDataTextureImpl _impl = new FloatDataTextureImpl();    // Mutable object, Don't change into readonly

        public ComponentOwner? Owner => _core.Owner;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        public FloatDataTexture()
        {
        }

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

    public struct FloatDataTextureImpl : IDisposable
    {
        public TextureObject TextureObject;     // TODO: public field はやめる
        public int Length;

        public unsafe void Load(ReadOnlySpan<Color4> texels)
        {
            if(!TextureObject.IsEmpty) {
                ThrowAlreadyLoaded();
                static void ThrowAlreadyLoaded() => throw new InvalidOperationException("Already loaded");
            }

            if(texels.IsEmpty) { return; }

            TextureObject = TextureObject.Create();
            Length = texels.Length;

            TextureObject.Bind1D(TextureObject);
            TextureObject.Parameter1DMinFilter(TextureShrinkMode.NearestNeighbor);
            TextureObject.Parameter1DMagFilter(TextureExpansionMode.NearestNeighbor);
            fixed(Color4* ptr = texels) {
                TextureObject.Image1D(texels.Length, ptr, TextureObject.InternalFormat.Rgba32f);
            }
            TextureObject.Unbind1D();
        }

        public unsafe void LoadUndefined(int width)
        {
            if(!TextureObject.IsEmpty) {
                ThrowAlreadyLoaded();
                static void ThrowAlreadyLoaded() => throw new InvalidOperationException("Already loaded");
            }
            if(width < 0) {
                ThrowOutOfRange();
                static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(width));
            }
            if(width == 0) { return; }
            TextureObject = TextureObject.Create();
            Length = width;

            TextureObject.Bind1D(TextureObject);
            TextureObject.Parameter1DMinFilter(TextureShrinkMode.NearestNeighbor);
            TextureObject.Parameter1DMagFilter(TextureExpansionMode.NearestNeighbor);
            TextureObject.Image1D(width, (Color4*)null, TextureObject.InternalFormat.Rgba32f);
            TextureObject.Unbind1D();
        }

        public unsafe void Update(ReadOnlySpan<Color4> texels, int xOffset)
        {
            if(TextureObject.IsEmpty) {
                ThrowNotYetLoaded();
                static void ThrowNotYetLoaded() => throw new InvalidOperationException("Cannnot update texels because not loaded yet.");
            }
            if((uint)xOffset >= (uint)Length || texels.Length > Length - xOffset) {
                ThrowOutOfRange($"Length: {Length}, {nameof(texels)}.Length: {texels.Length}, {nameof(xOffset)}: {xOffset}");
                static void ThrowOutOfRange(string msg) => throw new ArgumentOutOfRangeException(msg);
            }

            if(texels.IsEmpty) { return; }

            TextureObject.Bind1D(TextureObject);
            fixed(Color4* ptr = texels) {
                TextureObject.SubImage1D(xOffset, texels.Length, ptr);
            }
            TextureObject.Unbind1D();
        }

        public void Dispose()
        {
            TextureObject.Delete(ref TextureObject);
            Length = 0;
        }
    }
}
