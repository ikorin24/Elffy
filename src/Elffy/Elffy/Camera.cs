#nullable enable
using Elffy.Mathematics;
using Elffy.Exceptions;
using System;

namespace Elffy
{
    /// <summary>Camera class</summary>
    public class Camera
    {
        private const float NEAR = 3f;
        /// <summary>Aspect ratio (width / height)</summary>
        private float _aspect = 1f;

        /// <summary>Get or set position of the camera.</summary>
        public Vector3 Position
        {
            get { return _position; }
            set
            {
                _position = value;
                SetViewMatrix(_position, _direction, _up);
            }
        }
        private Vector3 _position = new Vector3(0, 0, 10);

        /// <summary>Get or set direction of the camera eye.</summary>
        public Vector3 Direction
        {
            get { return _direction; }
            set
            {
                if(value == Vector3.Zero) { return; }
                _direction = value;
                SetViewMatrix(_position, _direction, _up);
            }
        }
        private Vector3 _direction = -Vector3.UnitZ;

        /// <summary>Get or set direction of up.</summary>
        public Vector3 Up
        {
            get { return _up; }
            set
            {
                if(value == Vector3.Zero) { return; }
                _up = value;
                SetViewMatrix(_position, _direction, _up);
            }
        }
        private Vector3 _up = Vector3.UnitY;

        /// <summary>Get or set radian Y field of view of the camera. [0 ~ π]</summary>
        public float Fovy
        {
            get { return _fovy; }
            set
            {
                ArgumentChecker.ThrowArgumentIf(value <= 0 || value > MathTool.Pi, "Value must be 0 ~ π. (not include 0)");
                _fovy = value;
                SetProjectionMatrix(_fovy, _far, _aspect);
            }
        }
        private float _fovy = 25f.ToRadian();

        /// <summary>Get or set max distance from the camera.</summary>
        public float Far
        {
            get { return _far; }
            set
            {
                ArgumentChecker.ThrowArgumentIf(value <= NEAR, "Value must be bigger than 0. (or value is too small.)");
                _far = value;
                SetProjectionMatrix(_fovy, _far, _aspect);
            }
        }
        private float _far = 2000f;

        /// <summary>Get View Matrix</summary>
        internal Matrix4 View { get; private set; } = Matrix4.Identity;

        /// <summary>Get projection Matrix</summary>
        internal Matrix4 Projection { get; private set; } = Matrix4.Identity;

        /// <summary>Constructor</summary>
        internal Camera()
        {
            SetProjectionMatrix(_fovy, _far, _aspect);
            SetViewMatrix(_position, _direction, _up);
        }

        /// <summary>Look at the specified target position.</summary>
        /// <param name="target">position of target</param>
        public void LookAt(Vector3 target) => LookAt(target, Position);

        /// <summary>Look at the specified target position from specified camera position.</summary>
        /// <param name="target">position of target</param>
        /// <param name="cameraPos">position of camera</param>
        public void LookAt(Vector3 target, Vector3 cameraPos)
        {
            var vec = target - cameraPos;
            if(vec == Vector3.Zero) { return; }
            _direction = vec;
            _position = cameraPos;
            SetViewMatrix(_position, _direction, _up);
        }

        /// <summary>
        /// Change fovy with same screen region.<para/>
        /// [NOTE] Position of the camera is changed in this method.<para/>
        /// </summary>
        /// <param name="fovy">Y field of view radian. 0 ~ π</param>
        /// <param name="target">target position where screen region is same as current.</param>
        public void ChangeFovy(float fovy, Vector3 target)
        {
            ArgumentChecker.ThrowArgumentIf(fovy <= 0 || fovy > MathTool.Pi, $"{nameof(fovy)} must be 0 ~ π. (not include 0)");
            var pos = (1 - MathF.Tan(_fovy / 2f) / MathF.Tan(fovy / 2f)) * (target - Position);
            _position += pos;
            _fovy = fovy;
            SetProjectionMatrix(_fovy, _far, _aspect);
            SetViewMatrix(_position, _direction, _up);
        }

        /// <summary>描画先のサイズを変更します。<see cref="Projection"/> が変更されます</summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        internal void ChangeScreenSize(int width, int height)
        {
            ArgumentChecker.ThrowOutOfRangeIf(width < 0, nameof(width), width, "value is negative.");
            ArgumentChecker.ThrowOutOfRangeIf(height < 0, nameof(height), height, "value is negative");
            _aspect = (float)width / height;
            SetProjectionMatrix(_fovy, _far, _aspect);
        }

        private void SetViewMatrix(Vector3 pos, Vector3 dir, Vector3 up)
        {
            Matrix4.LookAt(pos, pos + dir, up, out var view);
            View = view;
        }

        private void SetProjectionMatrix(float radian, float far, float aspect)
        {
            if(aspect > 0) {
                Matrix4.PerspectiveProjection(radian, aspect, NEAR, far, out var projection);
                Projection = projection;
            }
        }
    }
}
