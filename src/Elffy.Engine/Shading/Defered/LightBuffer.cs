#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Elffy.Components.Implementation;
using Elffy.Effective;
using Elffy.Features;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading.Defered
{
    internal sealed class LightBuffer : IDisposable
    {
        private static Vector4 DefaultLightPosition => new Vector4(0, 500, 0, 1);
        private static Color4 DefaultLightColor => Color4.White;

        private IHostScreen? _screen;
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

        public bool TryGetHostScreen([MaybeNullWhen(false)] out IHostScreen screen)
        {
            screen = _screen;
            return _screen is not null;
        }

        public LightBufferData GetBufferData()
        {
            return new LightBufferData(_lightColors.TextureObject, _lightPositions.TextureObject, _lightCount);
        }

        public unsafe void Initialize(int lightCount)
        {
            const int Threshold = 16;

            if(lightCount <= Threshold) {
                Vector4* posPtr = stackalloc Vector4[Threshold];
                Color4* colorsPtr = stackalloc Color4[Threshold];
                FillAndInitialize(this, new Span<Vector4>(posPtr, lightCount), new Span<Color4>(colorsPtr, lightCount));
            }
            else {
                using var positionsBuf = new ValueTypeRentMemory<Vector4>(lightCount, false);
                using var colorsBuf = new ValueTypeRentMemory<Color4>(lightCount, false);
                FillAndInitialize(this, positionsBuf.AsSpan(), colorsBuf.AsSpan());
            }

            static void FillAndInitialize(LightBuffer lightBuffer, Span<Vector4> positions, Span<Color4> colors)
            {
                positions.Fill(DefaultLightPosition);
                colors.Fill(DefaultLightColor);
                lightBuffer.Initialize(positions, colors);
            }
        }

        public void Initialize(ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors)
        {
            var screen = Engine.CurrentContext;
            if(screen is null) {
                throw new InvalidOperationException();
            }
            if(positions.Length != colors.Length) {
                ThrowInvalidLength();
                [DoesNotReturn] static void ThrowInvalidLength() => throw new ArgumentException($"{nameof(positions)} and {nameof(colors)} must have same length.");
            }
            if(_initialized) {
                ThrowAlreadyInitialized();
            }
            CreateLightsBuffer(positions, colors, out _lightColors, out _lightPositions);
            ContextAssociatedMemorySafety.Register(this, screen);
            _screen = screen;
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
            if(disposing) {
                _lightColors.Dispose();
                _lightPositions.Dispose();
                _lightCount = 0;
                _disposed = true;
            }
            else {
                ContextAssociatedMemorySafety.OnFinalized(this);
            }
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
