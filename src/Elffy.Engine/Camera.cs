#nullable enable
using Elffy.Mathematics;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>Camera class</summary>
    public sealed class Camera
    {
        private EventSource<Camera> _matrixChanged;
        private Matrix4 _view;
        private Matrix4 _projection;
        private Vector3 _position;
        private Vector3 _direction;
        private Vector3 _up;
        private Frustum _frustum;
        private float _aspect;  // Aspect ratio (width / height). It may be NaN when height is 0.
        private float _near;
        private float _far;
        private CameraProjectionMode _projectionMode;
        private float _fovy;
        private float _height;
        public Event<Camera> MatrixChanged => _matrixChanged.Event;

        /// <summary>Get or set camera projection mode</summary>
        public CameraProjectionMode ProjectionMode
        {
            get => _projectionMode;
            set
            {
                if(_projectionMode == value) { return; }
                if(_projectionMode != CameraProjectionMode.Perspective && _projectionMode != CameraProjectionMode.Orthographic) {
                    ThrowArgument($"Invalid value of {nameof(CameraProjectionMode)}");
                }
                _projectionMode = value;
                UpdateProjectionMatrix();
                Frustum.FromMatrix(_projection, _view, out _frustum);
                _matrixChanged.Invoke(this);
            }
        }

        /// <summary>Get or set position of the camera.</summary>
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                CalcViewMatrix(_position, _direction, _up, out _view);
                Frustum.FromMatrix(_projection, _view, out _frustum);
                _matrixChanged.Invoke(this);
            }
        }

        /// <summary>Get or set direction of the camera eye.</summary>
        /// <remarks>The value is noamlized.</remarks>
        public Vector3 Direction
        {
            get => _direction;
            set
            {
                if(value == Vector3.Zero) { return; }
                _direction = value.Normalized();
                CalcViewMatrix(_position, _direction, _up, out _view);
                Frustum.FromMatrix(_projection, _view, out _frustum);
                _matrixChanged.Invoke(this);
            }
        }

        /// <summary>Get or set direction of up.</summary>
        /// <remarks>The value is noamlized.</remarks>
        public Vector3 Up
        {
            get => _up;
            set
            {
                if(value == Vector3.Zero) { return; }
                _up = value.Normalized();
                CalcViewMatrix(_position, _direction, _up, out _view);
                Frustum.FromMatrix(_projection, _view, out _frustum);
                _matrixChanged.Invoke(this);
            }
        }

        public ref readonly Frustum Frustum => ref _frustum;

        /// <summary>Get max distance from the camera. (Use <see cref="SetNearFar(float, float)"/> method to set the value.)</summary>
        public float Far => _far;

        /// <summary>Get min distance from the camera. (Use <see cref="SetNearFar(float, float)"/> method to set the value.)</summary>
        public float Near => _near;

        /// <summary>Get or set view matrix</summary>
        public ref readonly Matrix4 View => ref _view;

        /// <summary>Get or set projection matrix</summary>
        public ref readonly Matrix4 Projection => ref _projection;

        /// <summary>Constructor</summary>
        internal Camera()
        {
            _view = Matrix4.Identity;
            _projection = Matrix4.Identity;
            _position = new Vector3(0, 0, 10);
            _direction = -Vector3.UnitZ;
            _up = Vector3.UnitY;
            _near = 0.5f;
            _far = 2000f;
            _aspect = 1f;
            _fovy = 25f.ToRadian();
            _height = 10;

            UpdateProjectionMatrix();
            CalcViewMatrix(_position, _direction, _up, out _view);
            Frustum.FromMatrix(_projection, _view, out _frustum);
        }

        /// <summary>Try to get field of view Y in the case that <see cref="ProjectionMode"/> is <see cref="CameraProjectionMode.Perspective"/>, otherwise return false.</summary>
        /// <param name="fovy">field of view Y (It is 0 if the method returnes false)</param>
        /// <returns>succeeded or not</returns>
        public bool TryGetFovy(out float fovy)
        {
            if(_projectionMode == CameraProjectionMode.Perspective) {
                fovy = _fovy;
                return true;
            }
            fovy = 0;
            return false;
        }

        /// <summary>Try to set field of view Y in the case that <see cref="ProjectionMode"/> is <see cref="CameraProjectionMode.Perspective"/>, otherwise return false.</summary>
        /// <param name="fovy">field of view Y</param>
        /// <returns>succeeded or not</returns>
        public bool TrySetFovy(float fovy)
        {
            if(_projectionMode == CameraProjectionMode.Perspective) {
                if(fovy <= 0 || fovy > MathTool.Pi) { ThrowOutOfRange("Value must be 0 ~ π. (not include 0)"); }
                _fovy = fovy;
                UpdateProjectionMatrix();
                Frustum.FromMatrix(_projection, _view, out _frustum);
                _matrixChanged.Invoke(this);
                return true;
            }
            return false;
        }

        /// <summary>Try to get the height of the camera rect in the case that <see cref="ProjectionMode"/> is <see cref="CameraProjectionMode.Orthographic"/>, otherwise return false.</summary>
        /// <param name="height">height of the camera rect</param>
        /// <returns>succeeded or not</returns>
        public bool TryGetHeight(out float height)
        {
            if(_projectionMode == CameraProjectionMode.Orthographic) {
                height = _height;
                return true;
            }
            height = 0;
            return false;
        }

        /// <summary>Try to set the height of the camera rect in the case that <see cref="ProjectionMode"/> is <see cref="CameraProjectionMode.Orthographic"/>, otherwise return false.</summary>
        /// <param name="height">height of the camera rect</param>
        /// <returns>succeeded or not</returns>
        public bool TrySetHeight(float height)
        {
            if(_projectionMode == CameraProjectionMode.Orthographic) {
                _height = height;
                UpdateProjectionMatrix();
                Frustum.FromMatrix(_projection, _view, out _frustum);
                _matrixChanged.Invoke(this);
                return true;
            }
            return false;
        }

        /// <summary>Set values of near and far</summary>
        /// <param name="near"></param>
        /// <param name="far"></param>
        public void SetNearFar(float near, float far)
        {
            if(near <= 0) { ThrowOutOfRange("The value of near is 0 or negative."); }
            if(far <= 0) { ThrowOutOfRange("The value of far is 0 or negative."); }
            if(near > far) { ThrowOutOfRange("The value of near must be smaller than the value of far."); }
            _near = near;
            _far = far;
            UpdateProjectionMatrix();
            Frustum.FromMatrix(_projection, _view, out _frustum);
            _matrixChanged.Invoke(this);
        }

        /// <summary>Look at the specified target position.</summary>
        /// <param name="target">position of target</param>
        public void LookAt(in Vector3 target)
        {
            var vec = target - _position;
            if(vec == Vector3.Zero) { return; }
            _direction = vec.Normalized();
            CalcViewMatrix(_position, _direction, _up, out _view);
            Frustum.FromMatrix(_projection, _view, out _frustum);
            _matrixChanged.Invoke(this);
        }

        /// <summary>Look at the specified target position from specified camera position.</summary>
        /// <param name="target">position of target</param>
        /// <param name="cameraPos">position of camera</param>
        public void LookAt(in Vector3 target, in Vector3 cameraPos)
        {
            var vec = target - cameraPos;
            if(vec == Vector3.Zero) { return; }
            _direction = vec.Normalized();
            _position = cameraPos;
            CalcViewMatrix(_position, _direction, _up, out _view);
            Frustum.FromMatrix(_projection, _view, out _frustum);
            _matrixChanged.Invoke(this);
        }

        /// <summary>Change fovy with same screen region.</summary>
        /// <remarks>Position of the camera is changed in this method.</remarks>
        /// <param name="fovy">Y field of view radian. 0 ~ π</param>
        /// <param name="target">target position where screen region is same as current.</param>
        public void ChangeFovy(float fovy, in Vector3 target)
        {
            if(fovy <= 0 || fovy > MathTool.Pi) { ThrowOutOfRange($"{nameof(fovy)} must be 0 ~ π. (not include 0)"); }

            var pos = _position;
            var vec = (1 - MathF.Tan(_fovy / 2f) / MathF.Tan(fovy / 2f)) * (target - pos);
            _position = pos + vec;
            _fovy = fovy;
            UpdateProjectionMatrix();
            CalcViewMatrix(_position, _direction, _up, out _view);
            Frustum.FromMatrix(_projection, _view, out _frustum);
            _matrixChanged.Invoke(this);
        }

        /// <summary>Set screen size. (frame buffer size)</summary>
        /// <param name="width">frame buffer width</param>
        /// <param name="height">frame buffer height</param>
        internal void ChangeScreenSize(int width, int height)
        {
            if(width < 0) { ThrowOutOfRange($"{nameof(width)} is negative."); }
            if(height < 0) { ThrowOutOfRange($"{nameof(height)} is negative."); }

            _aspect = (float)width / height;
            UpdateProjectionMatrix();
            Frustum.FromMatrix(_projection, _view, out _frustum);
            _matrixChanged.Invoke(this);
        }

        private void UpdateProjectionMatrix()
        {
            if(_projectionMode == CameraProjectionMode.Perspective) {
                CalcPerspectiveProjection(_fovy, _near, _far, _aspect, ref _projection);
            }
            else if(_projectionMode == CameraProjectionMode.Orthographic) {
                CalcOrthographicProjection(_height, _near, _far, _aspect, ref _projection);
            }
            else {
                Debug.Fail("Invalid projection mode");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalcViewMatrix(in Vector3 pos, in Vector3 dir, in Vector3 up, out Matrix4 view)
        {
            Matrix4.LookAt(pos, pos + dir, up, out view);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalcPerspectiveProjection(float fovy, float near, float far, float aspect, ref Matrix4 projection)
        {
            // 'aspect' may be NaN, in which case the following condition will be false.
            if(aspect > 0) {
                Matrix4.PerspectiveProjection(fovy, aspect, near, far, out projection);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalcOrthographicProjection(float height, float near, float far, float aspect, ref Matrix4 projection)
        {
            // 'aspect' may be NaN, in which case the following condition will be false.
            if(aspect > 0) {
                var y = height / 2f;
                var x = y * aspect;
                Matrix4.OrthographicProjection(-x, x, -y, y, near, far, out projection);
            }
        }

        [DoesNotReturn]
        private static void ThrowOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);

        [DoesNotReturn]
        private static void ThrowArgument(string message) => throw new ArgumentException(message);
    }

    /// <summary>Camera projection mode</summary>
    public enum CameraProjectionMode : byte
    {
        Perspective = 0,
        Orthographic = 1,
    }
}
