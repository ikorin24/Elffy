#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elffy
{
    public sealed class LightManager
    {
        private readonly IHostScreen _screen;
        private readonly List<ILight> _lights = new List<ILight>();

        public IHostScreen Screen => _screen;
        public int LightCount => _lights.Count;

        internal LightManager(IHostScreen screen)
        {
            _screen = screen;
        }

        internal void RegisterLight(ILight light)
        {
            Debug.Assert(Engine.IsThreadMain);
            Debug.Assert(light is not null);
            Debug.Assert(light.LifeState == LifeState.New);
            _lights.Add(light);
        }

        internal void RemoveLight(ILight light)
        {
            Debug.Assert(Engine.IsThreadMain);
            Debug.Assert(light is not null);
            _lights.Remove(light);
        }

        public ReadOnlySpan<ILight> GetLights() => _lights.AsReadOnlySpan();

        internal void Release()
        {
            _lights.Clear();
        }
    }
}
