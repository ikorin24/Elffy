#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Elffy.Components.Implementation;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Features;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading
{
    internal sealed class LightBuffer : ILightBuffer, IDisposable
    {
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

        public void Initialize(ReadOnlySpan<LightData> lights)
        {
            var screen = Engine.CurrentContext;
            if(screen is null) {
                ContextMismatchException.ThrowCurrentContextIsNull();
            }
            if(_initialized) {
                ThrowAlreadyInitialized();
            }
            using var buf = SeparateLights(lights, out var positions, out var colors);
            InitializeCore(screen, positions, colors);
        }

        public void Initialize(ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors)
        {
            var screen = Engine.CurrentContext;
            if(screen is null) {
                ContextMismatchException.ThrowCurrentContextIsNull();
            }
            if(positions.Length != colors.Length) {
                ThrowInvalidLength();
                [DoesNotReturn] static void ThrowInvalidLength() => throw new ArgumentException($"{nameof(positions)} and {nameof(colors)} must have same length.");
            }
            if(_initialized) {
                ThrowAlreadyInitialized();
            }
            InitializeCore(screen, positions, colors);
        }

        private void InitializeCore(IHostScreen screen, ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors)
        {
            CreateLightsBuffer(positions, colors, out _lightColors, out _lightPositions);
            ContextAssociatedMemorySafety.Register(this, screen);
            _screen = screen;
            _lightCount = positions.Length;
            _initialized = true;
        }

        public void ReadPositions(Span<Vector4> positions)
        {
            if(_initialized == false) { ThrowNotInitialized(); }
            _lightPositions.Get(positions.MarshalCast<Vector4, Color4>());
        }

        public void ReadColors(Span<Color4> colors)
        {
            if(_initialized == false) { ThrowNotInitialized(); }
            _lightColors.Get(colors);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdatePositions(ReadOnlySpan<Vector4> positions, int offset)
        {
            if(_initialized == false) { ThrowNotInitialized(); }
            _lightPositions.Update(positions.MarshalCast<Vector4, Color4>(), offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateColors(ReadOnlySpan<Color4> colors, int offset)
        {
            if(_initialized == false) { ThrowNotInitialized(); }
            _lightColors.Update(colors, offset);
        }

        public void Update(ReadOnlySpan<LightData> lights, int offset)
        {
            if(_initialized == false) { ThrowNotInitialized(); }
            using var buf = SeparateLights(lights, out var pBuf, out var cBuf);
            _lightColors.Update(cBuf, offset);
            _lightPositions.Update(pBuf.MarshalCast<Vector4, Color4>(), offset);
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

        private static ValueTypeRentMemory<Vector4> SeparateLights(ReadOnlySpan<LightData> lights, out ReadOnlySpan<Vector4> positions, out ReadOnlySpan<Color4> colors)
        {
            var buf = new ValueTypeRentMemory<Vector4>(lights.Length * 2, false);
            try {
                var pBuf = buf.AsSpan(0, lights.Length);
                var cBuf = buf.AsSpan(lights.Length, lights.Length).MarshalCast<Vector4, Color4>();
                Debug.Assert(lights.Length == pBuf.Length);
                Debug.Assert(lights.Length == cBuf.Length);
                for(int i = 0; i < lights.Length; i++) {
                    pBuf.At(i) = lights[i].Position4;
                    cBuf.At(i) = lights[i].Color4;
                }
                positions = pBuf;
                colors = cBuf;
                return buf;
            }
            catch {
                buf.Dispose();
                throw;
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

    internal interface ILightBuffer
    {
        LightBufferData GetBufferData();
    }

    public readonly ref struct LightBufferData
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

        public void Deconstruct(out TextureObject colors, out TextureObject positions, out int lightCount)
        {
            colors = Colors;
            positions = Positions;
            lightCount = LightCount;
        }
    }
}
