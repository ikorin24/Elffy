#nullable enable
using System;
using Elffy.Graphics.OpenGL;
using Elffy.Effective;
using Elffy.Components.Implementation;
using Elffy.Features;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Components
{
    /// <summary>Float data texture component</summary>
    public sealed class FloatDataTexture : IContextAssociatedSafety
    {
        private bool _disposed;
        private ContextAssociatedSafetyImpl _safetyImpl;    // Mutable object, Don't change into readonly
        private FloatDataTextureCore _impl;                 // Mutable object, Don't change into readonly

        /// <summary>Get texture object of opengl</summary>
        /// <remarks>[NOTE] This is texture1D, NOT texture2D.</remarks>
        public TextureObject TextureObject => _impl.TextureObject;

        /// <summary>Get data length loaded in the texture.</summary>
        /// <remarks>[NOTE] This is the count of pixels in the texture, NOT the count of float values.</remarks>
        public int Length => _impl.Length;

        IHostScreen? IContextAssociatedSafety.AssociatedContext => _safetyImpl.AssociatedContext;

        /// <summary>Create new <see cref="FloatDataTexture"/></summary>
        public FloatDataTexture()
        {
        }

        ~FloatDataTexture() => Dispose(false);

        /// <summary>Load data to texture1D</summary>
        /// <param name="pixels"></param>
        public void Load(ReadOnlySpan<Vector4> pixels)
        {
            ThrowIfDisposed();
            _impl.Load(pixels.MarshalCast<Vector4, Color4>());
            _safetyImpl.TryRegisterToCurrentContext(this, static x => x.DisposeContextAssociatedMemory());
        }

        public void Load(ReadOnlySpan<Color4> pixels)
        {
            ThrowIfDisposed();
            _impl.Load(pixels);
            _safetyImpl.TryRegisterToCurrentContext(this, static x => x.DisposeContextAssociatedMemory());
        }

        public void LoadAsPowerOfTwo(ReadOnlySpan<Vector4> pixels)
        {
            ThrowIfDisposed();
            _impl.LoadAsPOT(pixels.MarshalCast<Vector4, Color4>());
            _safetyImpl.TryRegisterToCurrentContext(this, static x => x.DisposeContextAssociatedMemory());
        }

        public void LoadAsPowerOfTwo(ReadOnlySpan<Color4> pixels)
        {
            ThrowIfDisposed();
            _impl.LoadAsPOT(pixels);
            _safetyImpl.TryRegisterToCurrentContext(this, static x => x.DisposeContextAssociatedMemory());
        }

        public void Update(ReadOnlySpan<Vector4> pixels, int xOffset) => _impl.Update(pixels.MarshalCast<Vector4, Color4>(), xOffset);

        public void Update(ReadOnlySpan<Color4> pixels, int xOffset) => _impl.Update(pixels, xOffset);

        public void Dispose()
        {
            _safetyImpl.ThrowIfAssociatedContextMismatch();
            Dispose(true);
            GC.SuppressFinalize(this);
            _disposed = true;
        }

        private void Dispose(bool disposing)
        {
            if(disposing) {
                DisposeContextAssociatedMemory();
            }
            else {
                _safetyImpl.OnFinalized(this);
            }
        }

        private void DisposeContextAssociatedMemory()
        {
            _impl.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if(_disposed) {
                Throw();
                [DoesNotReturn] static void Throw() => throw new ObjectDisposedException(nameof(Texture));
            }
        }
    }
}
