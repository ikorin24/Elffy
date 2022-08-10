#nullable enable
using Elffy.Shading;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    public interface ILight
    {
        int Index { get; }
        short Token { get; }
        LifeState LifeState { get; }
        bool HasShadowMap { get; }
        Vector4 Position { get; set; }
        Color4 Color { get; set; }
        ref readonly Matrix4 LightMatrix { get; }
        ref readonly ShadowMapData ShadowMap { get; }
    }

    /// <summary>Implementation of <see cref="ILight"/></summary>
    internal readonly struct LightImpl : IEquatable<LightImpl>
    {
        private static readonly Vector4 ZeroVec4 = default;
        private static readonly Color4 ZeroColor4 = default;
        private static readonly Matrix4 ZeroMatrix4 = default;
        private static readonly ShadowMapData EmptyShadowMap = default;


        private readonly LightManager _lightManager;
        private readonly int _index;
        private readonly short _token;

        public LightManager Manager => _lightManager;

        public int IndexRaw => _index;
        public short Token => _token;

        public int Index
        {
            get
            {
                var manager = _lightManager;
                if(manager.ValidateToken(_index, _token) == false) {
                    return -1;
                }
                return _index;
            }
        }

        [Obsolete("Don't use default constructor.", true)]
        public LightImpl() => throw new NotSupportedException("Don't use default constructor.");

        public LightImpl(LightManager manager, int index, short token)
        {
            ArgumentNullException.ThrowIfNull(manager);
            _lightManager = manager;
            _index = index;
            _token = token;
        }

        public ref readonly Vector4 GetPositionOrZero()
        {
            var manager = _lightManager;
            if(manager.ValidateToken(_index, _token) == false) {
                return ref ZeroVec4;
            }
            return ref manager.GetPosition(_index);
        }

        public ref readonly Vector4 GetPositionOrNull()
        {
            var manager = _lightManager;
            if(manager.ValidateToken(_index, _token) == false) {
                return ref Unsafe.NullRef<Vector4>();
            }
            return ref manager.GetPosition(_index);
        }

        public bool TrySetPosition(in Vector4 value)
        {
            var manager = _lightManager;
            if(manager.ValidateToken(_index, _token) == false) {
                return false;
            }
            if(value == manager.GetPosition(_index)) { return false; }
            var positions = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in value), 1);
            _lightManager.UpdatePositions(positions, _index);
            return true;
        }

        public ref readonly Color4 GetColorOrZero()
        {
            var manager = _lightManager;
            if(manager.ValidateToken(_index, _token) == false) {
                return ref ZeroColor4;
            }
            return ref manager.GetColor(_index);
        }

        public ref readonly Color4 GetColorOrNull()
        {
            var manager = _lightManager;
            if(manager.ValidateToken(_index, _token) == false) {
                return ref Unsafe.NullRef<Color4>();
            }
            return ref manager.GetColor(_index);
        }

        public bool TrySetColor(in Color4 value)
        {
            var manager = _lightManager;
            if(manager.ValidateToken(_index, _token) == false) {
                return false;
            }
            if(value == manager.GetColor(_index)) { return false; }
            var colors = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in value), 1);
            _lightManager.UpdateColors(colors, _index);
            return true;
        }

        public ref readonly Matrix4 GetLightMatrixOrZero()
        {
            var manager = _lightManager;
            if(manager.ValidateToken(_index, _token) == false) {
                return ref ZeroMatrix4;
            }
            return ref manager.GetMatrix(_index);
        }

        public ref readonly Matrix4 GetLightMatrixOrNull()
        {
            var manager = _lightManager;
            if(manager.ValidateToken(_index, _token) == false) {
                return ref Unsafe.NullRef<Matrix4>();
            }
            return ref manager.GetMatrix(_index);
        }

        public ref readonly ShadowMapData GetShadowMapOrEmpty()
        {
            var manager = _lightManager;
            if(manager.ValidateToken(_index, _token) == false) {
                return ref EmptyShadowMap;
            }
            return ref manager.GetShadowMap(_index);
        }

        public ref readonly ShadowMapData GetShadowMapOrNull()
        {
            var manager = _lightManager;
            if(manager.ValidateToken(_index, _token) == false) {
                return ref Unsafe.NullRef<ShadowMapData>();
            }
            return ref manager.GetShadowMap(_index);
        }

        public bool ValidateToken() => _lightManager.ValidateToken(_index, _token);

        public override bool Equals(object? obj) => obj is LightImpl impl && Equals(impl);

        public bool Equals(LightImpl other)
            => _lightManager == other._lightManager &&
               _index == other._index &&
               _token == other._token;

        public override int GetHashCode() => HashCode.Combine(_lightManager, _index, _token);
    }
}
