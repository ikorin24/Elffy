#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>TRS helper implementation struct.</summary>
    /// <remarks>It is NOT thread-safe</remarks>
    /// <typeparam name="TOwner"></typeparam>
    internal struct Trs<TOwner> : IEquatable<Trs<TOwner>>
    {
        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _scale;
        private Matrix4 _transform;
        private bool _hasTransform;
        private EventSource<TOwner> _positionChanged;
        private EventSource<TOwner> _rotationChanged;
        private EventSource<TOwner> _scaleChanged;

        public Trs()
        {
            _position = Vector3.Zero;
            _rotation = Quaternion.Identity;
            _scale = Vector3.One;
            _transform = Matrix4.Identity;
            _hasTransform = true;
            _positionChanged = EventSource<TOwner>.Default;
            _rotationChanged = EventSource<TOwner>.Default;
            _scaleChanged = EventSource<TOwner>.Default;
        }

        [UnscopedRef]
        public readonly ref readonly Vector3 Position => ref _position;
        [UnscopedRef]
        public readonly ref readonly Quaternion Rotation => ref _rotation;
        [UnscopedRef]
        public readonly ref readonly Vector3 Scale => ref _scale;
        [UnscopedRef]
        public readonly ref readonly Matrix4 Transform => ref _transform;
        [UnscopedRef]
        public readonly Event<TOwner> PositionChanged => _positionChanged.Event;
        [UnscopedRef]
        public readonly Event<TOwner> RotationChanged => _rotationChanged.Event;
        [UnscopedRef]
        public readonly Event<TOwner> ScaleChanged => _scaleChanged.Event;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetPosition(in Vector3 value, out EventSource<TOwner> eventSource)
        {
            eventSource = _positionChanged;
            if(_position == value) {
                return false;
            }
            _hasTransform = false;
            _position = value;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetRotation(in Quaternion value, out EventSource<TOwner> eventSource)
        {
            eventSource = _rotationChanged;
            if(_rotation == value) {
                return false;
            }
            _hasTransform = false;
            _rotation = value;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetScale(in Vector3 value, out EventSource<TOwner> eventSource)
        {
            eventSource = _scaleChanged;
            if(_scale == value) {
                return false;
            }
            _hasTransform = false;
            _scale = value;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4 GetTransform()
        {
            if(_hasTransform == false) {
                UpdateTransform();
            }
            return _transform;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path
        private void UpdateTransform()
        {
            _transform = _position.ToTranslationMatrix4() * _rotation.ToMatrix4() * _scale.ToScaleMatrix4();
            _hasTransform = true;
        }

        public readonly override bool Equals(object? obj) => obj is Trs<TOwner> trs && Equals(trs);

        public readonly bool Equals(Trs<TOwner> other)
        {
            return _position.Equals(other._position) &&
                   _rotation.Equals(other._rotation) &&
                   _scale.Equals(other._scale) &&
                   _transform.Equals(other._transform) &&
                   _hasTransform == other._hasTransform &&
                   _positionChanged.Equals(other._positionChanged) &&
                   _rotationChanged.Equals(other._rotationChanged) &&
                   _scaleChanged.Equals(other._scaleChanged);
        }

        public readonly override int GetHashCode() => HashCode.Combine(_position, _rotation, _scale, _transform, _hasTransform, _positionChanged, _rotationChanged, _scaleChanged);
    }
}
