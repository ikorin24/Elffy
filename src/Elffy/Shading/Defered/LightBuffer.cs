#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Elffy.Components.Implementation;
using Elffy.Effective;
using Elffy.OpenGL;

namespace Elffy.Shading.Defered
{
    internal sealed class LightBuffer : ILightBuffer, IDisposable
    {
        private FloatDataTextureCore _lightColors;
        private FloatDataTextureCore _lightPositions;
        private int _lightCount;
        private bool _initialized;
        private bool _disposed;

        public int LightCount => _lightCount;

        public bool IsInitialized => _initialized;

        public bool IsDisposed => _disposed;

        internal LightBuffer()
        {
        }

        ~LightBuffer() => Dispose(false);

        public LightBufferData GetBufferData()
        {
            return new LightBufferData(_lightColors.TextureObject, _lightPositions.TextureObject, _lightCount);
        }

        public void Initialize(ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors)
        {
            if(positions.Length != colors.Length) {
                ThrowInvalidLength();
                [DoesNotReturn] static void ThrowInvalidLength() => throw new ArgumentException($"{nameof(positions)} and {nameof(colors)} must have same length.");
            }
            if(_initialized) {
                ThrowAlreadyInitialized();
            }
            CreateLightsBuffer(positions, colors, out _lightColors, out _lightPositions);
            _lightCount = positions.Length;
            _initialized = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdatePositions(ReadOnlySpan<Vector4> positions, int offset)
        {
            if(_initialized == false) { ThrowNotInitialized(); }
            _lightPositions.Update(positions.MarshalCast<Vector4, Color4>(), offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateColors(ReadOnlySpan<Color4> positions, int offset)
        {
            if(_initialized == false) { ThrowNotInitialized(); }
            _lightColors.Update(positions, offset);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            _lightColors.Dispose();
            _lightPositions.Dispose();
            _lightCount = 0;
            _disposed = true;
        }

        private unsafe static void CreateLightsBuffer(ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors,
                                                      out FloatDataTextureCore lightColorsTexture, out FloatDataTextureCore lightPositionsTexture)
        {
            Debug.Assert(positions.Length == colors.Length);
            lightColorsTexture = new();
            lightPositionsTexture = new();
            try {
                lightColorsTexture.LoadAsPOT(colors);
                lightPositionsTexture.LoadAsPOT(positions.MarshalCast<Vector4, Color4>());
            }
            catch {
                lightColorsTexture.Dispose();
                lightPositionsTexture.Dispose();
                throw;
            }
        }

        [DoesNotReturn]
        private static void ThrowAlreadyInitialized() => throw new InvalidOperationException($"{nameof(LightBuffer)} is already initialized.");

        [DoesNotReturn]
        private static void ThrowNotInitialized() => throw new InvalidOperationException($"{nameof(LightBuffer)} is not initialized.");
    }

    internal interface ILightBuffer
    {
        LightBufferData GetBufferData();
    }

    internal readonly ref struct LightBufferData
    {
        public readonly TextureObject Colors;
        public readonly TextureObject Positions;
        public readonly int LightCount;

        public LightBufferData(in TextureObject colors, in TextureObject positions, int lightCount)
        {
            Colors = colors;
            Positions = positions;
            LightCount = lightCount;
        }
    }
}
