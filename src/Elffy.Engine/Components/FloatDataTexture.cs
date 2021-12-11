#nullable enable
using System;
using Elffy.Graphics.OpenGL;
using Elffy.Effective;
using Elffy.Components.Implementation;
using Elffy.Features;

namespace Elffy.Components
{
    /// <summary>Float data texture component</summary>
    public class FloatDataTexture : ISingleOwnerComponent
    {
        private SingleOwnerComponentCore _core; // Mutable object, Don't change into readonly
        private FloatDataTextureCore _impl;     // Mutable object, Don't change into readonly

        /// <inheritdoc/>
        public ComponentOwner? Owner => _core.Owner;

        /// <inheritdoc/>
        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        /// <summary>Get texture object of opengl</summary>
        /// <remarks>[NOTE] This is texture1D, NOT texture2D.</remarks>
        public TextureObject TextureObject => _impl.TextureObject;

        /// <summary>Get data length loaded in the texture.</summary>
        /// <remarks>[NOTE] This is the count of pixels in the texture, NOT the count of float values.</remarks>
        public int Length => _impl.Length;

        /// <summary>Create new <see cref="FloatDataTexture"/></summary>
        public FloatDataTexture()
        {
        }

        ~FloatDataTexture() => Dispose(false);

        /// <summary>Load data to texture1D</summary>
        /// <param name="pixels"></param>
        public void Load(ReadOnlySpan<Vector4> pixels)
        {
            _impl.Load(pixels.MarshalCast<Vector4, Color4>());
            ContextAssociatedMemorySafety.Register(this, Engine.CurrentContext!);
        }

        public void Load(ReadOnlySpan<Color4> pixels)
        {
            _impl.Load(pixels);
            ContextAssociatedMemorySafety.Register(this, Engine.CurrentContext!);
        }

        public void LoadAsPowerOfTwo(ReadOnlySpan<Vector4> pixels)
        {
            _impl.LoadAsPOT(pixels.MarshalCast<Vector4, Color4>());
            ContextAssociatedMemorySafety.Register(this, Engine.CurrentContext!);
        }

        public void LoadAsPowerOfTwo(ReadOnlySpan<Color4> pixels)
        {
            _impl.LoadAsPOT(pixels);
            ContextAssociatedMemorySafety.Register(this, Engine.CurrentContext!);
        }

        public void Update(ReadOnlySpan<Vector4> pixels, int xOffset) => _impl.Update(pixels.MarshalCast<Vector4, Color4>(), xOffset);

        public void Update(ReadOnlySpan<Color4> pixels, int xOffset) => _impl.Update(pixels, xOffset);

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing) {
                _impl.Dispose();
            }
            else {
                ContextAssociatedMemorySafety.OnFinalized(this);
            }
        }

        void IComponent.OnAttached(ComponentOwner owner) => _core.OnAttached(owner, this);

        void IComponent.OnDetached(ComponentOwner owner) => _core.OnDetached(owner, this);
    }
}
