#nullable enable
using Elffy.Core;
using Elffy.OpenGL;
using Elffy.Effective;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Components
{
    /// <summary>Float data texture component</summary>
    public class FloatDataTexture : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore _core = new(true);                 // Mutable object, Don't change into readonly
        private FloatDataTextureImpl _impl = new FloatDataTextureImpl();    // Mutable object, Don't change into readonly

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

        public void Update(ReadOnlySpan<Vector4> pixels, int xOffset) => _impl.Update(pixels.MarshalCast<Vector4, Color4>(), xOffset);

        public void Update(ReadOnlySpan<Color4> pixels, int xOffset) => _impl.Update(pixels, xOffset);

        public void Dispose()
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

        public void OnAttached(ComponentOwner owner) => OnAttachedCore<FloatDataTexture>(owner, this);

        public void OnDetached(ComponentOwner owner) => OnDetachedCore<FloatDataTexture>(owner, this);

        protected void OnAttachedCore<T>(ComponentOwner owner, T @this) where T : FloatDataTexture
        {
            _core.OnAttached<T>(owner, @this);
        }

        protected void OnDetachedCore<T>(ComponentOwner owner, T @this) where T : FloatDataTexture
        {
            _core.OnDetachedForDisposable<T>(owner, @this);
        }
    }

    /// <summary>Float data texture implementation struct</summary>
    public struct FloatDataTextureImpl : IDisposable
    {
        private TextureObject _to;
        private int _length;

        /// <summary>Get texture object of opengl</summary>
        /// <remarks>[NOTE] This is texture1D, NOT texture2D.</remarks>
        public readonly TextureObject TextureObject => _to;

        /// <summary>Get data length loaded in the texture.</summary>
        /// <remarks>[NOTE] This is the count of pixels in the texture, NOT the count of float values.</remarks>
        public readonly int Length => _length;

        /// <summary>Allocate memory and load pixels to the texture1D</summary>
        /// <param name="pixels">pixels to load</param>
        public unsafe void Load(ReadOnlySpan<Color4> pixels)
        {
            if(!TextureObject.IsEmpty) {
                ThrowAlreadyLoaded();
                [DoesNotReturn] static void ThrowAlreadyLoaded() => throw new InvalidOperationException("Already loaded");
            }

            if(pixels.IsEmpty) { return; }

            _to = TextureObject.Create();
            _length = pixels.Length;

            TextureObject.Bind1D(_to);
            SetTextureParams();
            fixed(Color4* ptr = pixels) {
                TextureObject.Image1D(pixels.Length, ptr, TextureObject.InternalFormat.Rgba32f);
            }
            TextureObject.Unbind1D();
        }

        /// <summary>Allocate memory of the texture1D but not load pixels.</summary>
        /// <param name="width">count of the pixels. (That means the width of the texture1D.)</param>
        public unsafe void LoadUndefined(int width)
        {
            if(!TextureObject.IsEmpty) {
                ThrowAlreadyLoaded();
                [DoesNotReturn] static void ThrowAlreadyLoaded() => throw new InvalidOperationException("Already loaded");
            }
            if(width < 0) {
                ThrowOutOfRange();
                [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(width));
            }
            if(width == 0) { return; }
            _to = TextureObject.Create();
            _length = width;

            TextureObject.Bind1D(_to);
            SetTextureParams();
            TextureObject.Image1D(width, (Color4*)null, TextureObject.InternalFormat.Rgba32f);
            TextureObject.Unbind1D();
        }

        /// <summary>Update subpixels of the texture1D</summary>
        /// <param name="pixels">pixels to update</param>
        /// <param name="xOffset">offset pixels count of the beginning of the range to update</param>
        public unsafe void Update(ReadOnlySpan<Color4> pixels, int xOffset)
        {
            if(TextureObject.IsEmpty) {
                ThrowNotYetLoaded();
                [DoesNotReturn] static void ThrowNotYetLoaded() => throw new InvalidOperationException("Cannnot update texels because not loaded yet.");
            }
            if((uint)xOffset >= (uint)Length || pixels.Length > Length - xOffset) {
                ThrowOutOfRange($"Length: {Length}, {nameof(pixels)}.Length: {pixels.Length}, {nameof(xOffset)}: {xOffset}");
                [DoesNotReturn] static void ThrowOutOfRange(string msg) => throw new ArgumentOutOfRangeException(msg);
            }

            if(pixels.IsEmpty) { return; }

            TextureObject.Bind1D(TextureObject);
            fixed(Color4* ptr = pixels) {
                TextureObject.SubImage1D(xOffset, pixels.Length, ptr);
            }
            TextureObject.Unbind1D();
        }

        private void SetTextureParams()
        {
            TextureObject.Parameter1DMinFilter(TextureShrinkMode.NearestNeighbor, TextureMipmapMode.None);
            TextureObject.Parameter1DMagFilter(TextureExpansionMode.NearestNeighbor);
            TextureObject.Parameter1DWrapS(TextureWrapMode.ClampToEdge);
            TextureObject.Parameter1DWrapT(TextureWrapMode.ClampToEdge);
        }

        /// <summary>Dispose the texture1D</summary>
        public void Dispose()
        {
            TextureObject.Delete(ref _to);
            _length = 0;
        }
    }
}
