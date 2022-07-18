#nullable enable
using Elffy.Effective;
using Elffy.Features;
using Elffy.Graphics.OpenGL;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Shading
{
    public sealed class LightManager
    {
        private const int ShadowMapSize = 2048;

        private readonly IHostScreen _screen;
        private readonly LightBuffer _lights;
        private ValueTypeRentMemory<ShadowMapData> _shadowMaps;

        public IHostScreen Screen => _screen;

        public int LightCount => _lights.LightCount;

        public Light this[int index]
        {
            get
            {
                if((uint)index >= (uint)LightCount) {
                    ThrowOutOfRange();
                    [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(index));
                }
                return new Light(this, index);
            }
        }

        public TextureObject PositionTexture => _lights.PositionTexture;
        public TextureObject ColorTexture => _lights.ColorTexture;
        public TextureObject MatrixTexture => _lights.MatrixTexture;

        internal LightManager(IHostScreen screen)
        {
            _screen = screen;
            _lights = new LightBuffer();
            _shadowMaps = ValueTypeRentMemory<ShadowMapData>.Empty;
        }

        public void Initialize(ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors)
        {
            _lights.Initialize(positions, colors);

            var count = positions.Length;
            var shadowMaps = ValueTypeRentMemory<ShadowMapData>.Empty;
            try {
                shadowMaps = new ValueTypeRentMemory<ShadowMapData>(count, true);
                var span = shadowMaps.AsSpan();
                for(int i = 0; i < span.Length; i++) {
                    var sm = new ShadowMapData();
                    sm.Initialize(ShadowMapSize, ShadowMapSize);
                    span[i] = sm;
                }
            }
            catch {
                foreach(var item in shadowMaps.AsSpan()) {
                    item.Release();
                }
                shadowMaps.Dispose();
                throw;
            }
            _shadowMaps = shadowMaps;
        }

        public ReadOnlySpan<Vector4> GetPositions() => _lights.GetPositions();

        public ReadOnlySpan<Color4> GetColors() => _lights.GetColors();

        public ReadOnlySpan<Matrix4> GetMatrices() => _lights.GetMatrices();

        public ReadOnlySpan<ShadowMapData> GetShadowMaps() => _shadowMaps.AsSpan();

        public void UpdatePositions(ReadOnlySpan<Vector4> positions, int offset) => _lights.UpdatePositions(positions, offset);

        public void UpdatePositions(SpanUpdateAction<Vector4> action) => _lights.UpdatePositions(action);

        public void UpdatePositions<TArg>(TArg arg, SpanUpdateAction<Vector4, TArg> action) => _lights.UpdatePositions(arg, action);

        public void UpdateColors(ReadOnlySpan<Color4> colors, int offset) => _lights.UpdateColors(colors, offset);

        public void UpdateColors(SpanUpdateAction<Color4> action) => _lights.UpdateColors(action);

        public void UpdateColors<TArg>(TArg arg, SpanUpdateAction<Color4, TArg> action) => _lights.UpdateColors(arg, action);

        internal void Release()
        {
            _lights.Dispose();
            foreach(var map in _shadowMaps.AsSpan()) {
                map.Release();
            }
            _shadowMaps.Dispose();
        }
    }

    public readonly struct Light : IEquatable<Light>
    {
        private readonly LightManager _lightManager;
        private readonly int _index;

        public ref readonly Vector4 Position => ref _lightManager.GetPositions()[_index];

        public ref readonly Color4 Color => ref _lightManager.GetColors()[_index];

        public ref readonly Matrix4 LightMatrix => ref _lightManager.GetMatrices()[_index];

        public ref readonly ShadowMapData ShadowMap => ref _lightManager.GetShadowMaps()[_index];

        internal Light(LightManager lightManager, int index)
        {
            _lightManager = lightManager;
            _index = index;
        }

        public override bool Equals(object? obj) => obj is Light light && Equals(light);

        public bool Equals(Light other) => (_lightManager == other._lightManager) && (_index == other._index);

        public override int GetHashCode() => HashCode.Combine(_lightManager, _index);
    }
}
