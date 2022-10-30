#nullable enable
using Elffy.Shading;
using System;

namespace Elffy
{
    public interface ILight
    {
        int Index { get; }
        short Token { get; }
        LifeState LifeState { get; }
        Vector4 Position { get; set; }
        Color4 Color { get; set; }
        RefReadOnlyOrNull<Matrix4> LightMatrix { get; }
        RefReadOnlyOrNull<ShadowMapData> ShadowMap { get; }
    }

    /// <summary>Implementation of <see cref="ILight"/></summary>
    internal readonly struct LightImpl : IEquatable<LightImpl>
    {
        private readonly LightManager _manager;
        private readonly int _index;
        private readonly short _token;

        public LightManager Manager => _manager;

        public int IndexRaw => _index;
        public short Token => _token;

        public int Index => ValidateToken() ? _index : -1;

        [Obsolete("Don't use default constructor.", true)]
        public LightImpl() => throw new NotSupportedException("Don't use default constructor.");

        public LightImpl(LightManager manager, int index, short token)
        {
            ArgumentNullException.ThrowIfNull(manager);
            _manager = manager;
            _index = index;
            _token = token;
        }

        public RefReadOnlyOrNull<Vector4> GetPosition() => ValidateToken() ?
            _manager.GetPosition(_index) :
            RefReadOnlyOrNull<Vector4>.NullRef;

        public bool TrySetPosition(in Vector4 value)
        {
            if(ValidateToken() == false) {
                return false;
            }
            if(value == _manager.GetPosition(_index).Derefer()) { return false; }
            _manager.UpdatePosition(in value, _index);
            return true;
        }

        public RefReadOnlyOrNull<Color4> GetColor() => ValidateToken() ?
            _manager.GetColor(_index) :
            RefReadOnlyOrNull<Color4>.NullRef;

        public bool TrySetColor(in Color4 value)
        {
            if(ValidateToken() == false) {
                return false;
            }
            if(value == _manager.GetColor(_index).Derefer()) { return false; }
            _manager.UpdateColor(in value, _index);
            return true;
        }

        public RefReadOnlyOrNull<Matrix4> GetLightMatrix() => ValidateToken() ?
            _manager.GetMatrix(_index) :
            RefReadOnlyOrNull<Matrix4>.NullRef;

        public RefReadOnlyOrNull<ShadowMapData> GetShadowMap() => ValidateToken() ?
            _manager.GetShadowMap(_index) :
            RefReadOnlyOrNull<ShadowMapData>.NullRef;

        public bool ValidateToken() => _manager.ValidateToken(_index, _token);

        public override bool Equals(object? obj) => obj is LightImpl impl && Equals(impl);

        public bool Equals(LightImpl other)
            => _manager == other._manager &&
               _index == other._index &&
               _token == other._token;

        public override int GetHashCode() => HashCode.Combine(_manager, _index, _token);
    }
}
