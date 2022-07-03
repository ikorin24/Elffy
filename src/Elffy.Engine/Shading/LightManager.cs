#nullable enable
using Elffy.Features;
using System;

namespace Elffy.Shading
{
    public sealed class LightManager
    {
        private readonly IHostScreen _screen;
        private readonly LightBuffer _lights;

        public IHostScreen Screen => _screen;

        internal LightManager(IHostScreen screen)
        {
            _screen = screen;
            _lights = new LightBuffer();
        }

        public void Initialize(ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors) => _lights.Initialize(positions, colors);

        public LightBufferData GetBufferData() => _lights.GetBufferData();

        public ReadOnlySpan<Vector4> GetPositions() => _lights.GetPositions();

        public ReadOnlySpan<Color4> GetColors() => _lights.GetColors();

        public void UpdatePositions(ReadOnlySpan<Vector4> positions, int offset) => _lights.UpdatePositions(positions, offset);

        public void UpdatePositions(SpanUpdateAction<Vector4> action) => _lights.UpdatePositions(action);

        public void UpdatePositions<TArg>(TArg arg, SpanUpdateAction<Vector4, TArg> action) => _lights.UpdatePositions(arg, action);

        public void UpdateColors(ReadOnlySpan<Color4> colors, int offset) => _lights.UpdateColors(colors, offset);

        public void UpdateColors(SpanUpdateAction<Color4> action) => _lights.UpdateColors(action);

        public void UpdateColors<TArg>(TArg arg, SpanUpdateAction<Color4, TArg> action) => _lights.UpdateColors(arg, action);

        internal void ReleaseBuffer()
        {
            _lights.Dispose();
        }
    }
}
