#nullable enable

namespace Elffy.Shading
{
    public sealed class LightManager
    {
        private readonly IHostScreen _screen;
        private readonly StaticLightManager _staticLights;

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
}
