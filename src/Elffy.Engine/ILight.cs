#nullable enable
using Elffy.Shading;
using System;

namespace Elffy
{
    public interface ILight
    {
        LifeState LifeState { get; }
        Vector4 Position { get; set; }
        Color4 Color { get; set; }
        RefReadOnly<Matrix4> LightMatrix { get; }
        RefReadOnly<ShadowMapData> ShadowMap { get; }
    }
}
