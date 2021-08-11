#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elffy.Components;
using Elffy.Effective;
using Elffy.OpenGL;

namespace Elffy.Shading
{
    internal sealed class LightBuffer : ILightBuffer, IDisposable
    {
        private FloatDataTextureImpl _lights;
        private FloatDataTextureImpl _lightPositions;
        private int _lightCount;
        private bool _initialized;

        public LightBuffer()
        {
        }

        ~LightBuffer() => Dispose(false);

        public LightBufferData GetBufferData()
        {
            return new LightBufferData(_lights.TextureObject, _lightPositions.TextureObject, _lightCount);
        }

        public void Initialize(ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors)
        {
            if(positions.Length != colors.Length) {
                ThrowInvalidLength();
                [DoesNotReturn] static void ThrowInvalidLength() => throw new ArgumentException($"{nameof(positions)} and {nameof(colors)} must have same length.");
            }
            if(_initialized) {
                ThrowNotInitialized();
            }
            CreateLightsBuffer(positions, colors, out _lights, out _lightPositions);
            _lightCount = positions.Length;
            _initialized = true;
        }

        //public void UpdateLightPositions(ReadOnlySpan<Vector4> positions, int offset)
        //{
        //    if(_initialized) {
        //        ThrowNotInitialized();
        //    }
        //    _lightPositions.Update(positions.MarshalCast<Vector4, Color4>(), offset);
        //}

        //public void UpdateLightColors(ReadOnlySpan<Color4> colors, int offset)
        //{
        //    if(_initialized) {
        //        ThrowNotInitialized();
        //    }
        //    _lights.Update(colors, offset);
        //}

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            _lights.Dispose();
            _lightPositions.Dispose();
            _lightCount = 0;
        }

        private unsafe static void CreateLightsBuffer(ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors,
                                                      out FloatDataTextureImpl lightColorsTexture, out FloatDataTextureImpl lightPositionsTexture)
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
        private static void ThrowNotInitialized() => throw new InvalidOperationException($"{nameof(GBuffer)} is not initialized.");
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
