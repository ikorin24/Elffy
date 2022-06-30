#nullable enable
using Elffy.Mathematics;
using Elffy.Graphics.OpenGL;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Components.Implementation
{
    /// <summary>Float data texture implementation struct</summary>
    public struct FloatDataTextureCore : IEquatable<FloatDataTextureCore>, IDisposable
    {
        private TextureObject _to;
        private int _length;

        /// <summary>Get texture object of opengl</summary>
        /// <remarks>[NOTE] This is texture1D, NOT texture2D.</remarks>
        public readonly TextureObject TextureObject => _to;

        /// <summary>Get data length loaded in the texture.</summary>
        /// <remarks>[NOTE] This is the count of pixels in the texture, NOT the count of float values.</remarks>
        public readonly int Length => _length;

        public unsafe void Get(Span<Color4> pixels)
        {
            if(TextureObject.IsEmpty) {
                ThrowNotLoaded();
            }
            if(pixels.IsEmpty) { return; }

            TextureObject.Bind1D(_to);
            fixed(Color4* p = pixels) {
                TextureObject.GetImage1D(p);
            }
            TextureObject.Unbind1D();
        }

        /// <summary>Allocate memory and load pixels to the texture1D</summary>
        /// <param name="pixels">pixels to load</param>
        public unsafe void Load(ReadOnlySpan<Color4> pixels)
        {
            if(!TextureObject.IsEmpty) {
                ThrowAlreadyLoaded();
            }

            if(pixels.IsEmpty) { return; }

            _to = TextureObject.Create();
            _length = pixels.Length;

            TextureObject.Bind1D(_to);
            SetTextureParams();
            fixed(Color4* ptr = pixels) {
                TextureObject.Image1D(pixels.Length, ptr, TextureInternalFormat.Rgba32f, 0);
            }
            TextureObject.Unbind1D();
        }

        /// <summary>Allocate memory by rounding up the length power of two, and load pixels to the texture1D.</summary>
        /// <remarks>The number of pixels will be rounded up to the power of two.</remarks>
        /// <param name="pixels">pixels to load</param>
        /// <returns>The rounded up number</returns>
        public unsafe int LoadAsPOT(ReadOnlySpan<Color4> pixels)
        {
            if(!TextureObject.IsEmpty) {
                ThrowAlreadyLoaded();
            }
            // Round up the length at first because it may throw an exception.
            var lengthPowerOfTwo = MathTool.RoundUpToPowerOfTwo(pixels.Length);

            _to = TextureObject.Create();
            _length = lengthPowerOfTwo;
            TextureObject.Bind1D(_to);
            SetTextureParams();

            // Allocate memory of power of two
            TextureObject.Image1D(lengthPowerOfTwo, (Color4*)null, TextureInternalFormat.Rgba32f, 0);
            fixed(Color4* ptr = pixels) {
                // Send pixels of actual length
                TextureObject.SubImage1D(0, pixels.Length, ptr, 0);
            }
            TextureObject.Unbind1D();
            return lengthPowerOfTwo;
        }

        /// <summary>Allocate memory of the texture1D but not load pixels.</summary>
        /// <param name="width">count of the pixels. (That means the width of the texture1D.)</param>
        public unsafe void LoadUndefined(int width)
        {
            if(!TextureObject.IsEmpty) {
                ThrowAlreadyLoaded();
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
            TextureObject.Image1D(width, (Color4*)null, TextureInternalFormat.Rgba32f, 0);
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
                TextureObject.SubImage1D(xOffset, pixels.Length, ptr, 0);
            }
            TextureObject.Unbind1D();
        }

        private void SetTextureParams()
        {
            TextureObject.Parameter1DMinFilter(TextureShrinkMode.NearestNeighbor, TextureMipmapMode.None);
            TextureObject.Parameter1DMagFilter(TextureExpansionMode.NearestNeighbor);
            TextureObject.Parameter1DWrapS(TextureWrapMode.ClampToEdge);
        }

        /// <summary>Dispose the texture1D</summary>
        public void Dispose()
        {
            TextureObject.Delete(ref _to);
            _length = 0;
        }

        [DoesNotReturn]
        private static void ThrowAlreadyLoaded() => throw new InvalidOperationException("The texture is already loaded");

        [DoesNotReturn]
        private static void ThrowNotLoaded() => throw new InvalidOperationException("The texture is not loaded");

        public override bool Equals(object? obj) => obj is FloatDataTextureCore core && Equals(core);

        public bool Equals(FloatDataTextureCore other) => _to.Equals(other._to) && _length == other._length;

        public override int GetHashCode() => HashCode.Combine(_to, _length);
    }
}
