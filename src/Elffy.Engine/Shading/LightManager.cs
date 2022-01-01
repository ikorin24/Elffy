#nullable enable
using System;

namespace Elffy.Shading
{
    public sealed class LightManager
    {
        private IHostScreen _screen;
        private StaticLightManager _staticLights;

        public IHostScreen Screen => _screen;

        public StaticLightManager StaticLights => _staticLights;

        internal LightManager(IHostScreen screen)
        {
            _screen = screen;
            _staticLights = new StaticLightManager(this);
        }

        internal void ReleaseBuffer()
        {
            _staticLights.ReleaseBuffer();
        }
    }

    public sealed class StaticLightManager
    {
        private readonly LightManager _lightManager;
        private readonly LightBuffer _lightBuffer;

        public IHostScreen Screen => _lightManager.Screen;

        public int LightCount => _lightBuffer.LightCount;

        internal ILightBuffer LightBuffer => _lightBuffer;

        internal StaticLightManager(LightManager lightManager)
        {
            _lightManager = lightManager;
            _lightBuffer = new LightBuffer();
        }

        public void Initialize()
        {
            ReadOnlySpan<Vector4> positions = stackalloc Vector4[1]
            {
                new Vector4(1, 1, 1, 0),
            };
            ReadOnlySpan<Color4> colors = stackalloc Color4[1]
            {
                Color4.White,
            };
            _lightBuffer.Initialize(positions, colors);
        }

        public void Initialize(ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors) => _lightBuffer.Initialize(positions, colors);

        internal void ReleaseBuffer()
        {
            _lightBuffer.Dispose();
        }

        public LightUpdateContext StartUpdate(LightUpdateMode mode = LightUpdateMode.ReadWrite) => new(_lightBuffer, mode);
    }
}
