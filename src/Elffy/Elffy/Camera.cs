using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy.Core;
using OpenTK;
using Elffy.Mathmatics;
using Elffy.Exceptions;

namespace Elffy
{
    public class Camera
    {
        private const float NEAR = 0.3f;
        private float _aspect = 1f;

        #region Position
        public Vector3 Position
        {
            get { return _position; }
            set
            {
                _position = value;
                SetMatrix(_position, _direction, _up);
            }
        }
        private Vector3 _position = new Vector3(0, 0, 10);
        #endregion

        #region Direction
        public Vector3 Direction
        {
            get { return _direction; }
            set
            {
                ArgumentChecker.ThrowIf(value.X == 0 && value.Y == 0 && value.Z == 0, new ArgumentException("Value must be non-Zero vector."));
                _direction = value;
                SetMatrix(_position, _direction, _up);
            }
        }
        private Vector3 _direction = -Vector3.UnitZ;
        #endregion

        #region Up
        public Vector3 Up
        {
            get { return _up; }
            set
            {
                ArgumentChecker.ThrowIf(value.X == 0 && value.Y == 0 && value.Z == 0, new ArgumentException("Value must be non-Zero vector."));
                _up = value;
                SetMatrix(_position, _direction, _up);
            }
        }
        private Vector3 _up = Vector3.UnitY;
        #endregion

        #region Fovy
        /// <summary>Y方向視野角(ラジアン)[0 ~ π]</summary>
        public float Fovy
        {
            get { return _fovy; }
            set
            {
                ArgumentChecker.ThrowIf(value <= 0 || value > MathTool.Pi, new ArgumentException("Value must be 0 ~ π. (not include 0)"));
                _fovy = value;
                SetProjection(_fovy, _far, _aspect);
            }
        }
        private float _fovy = 25f.ToRadian();
        #endregion

        #region Far
        public float Far
        {
            get { return _far; }
            set
            {
                ArgumentChecker.ThrowIf(value <= NEAR, new ArgumentException("Value must be bigger than 0. (or value is too small.)"));
                _far = value;
                SetProjection(_fovy, _far, _aspect);
            }
        }
        private float _far = 2000f;
        #endregion

        #region internal Property
        internal Matrix4 View { get; private set; } = Matrix4.Identity;

        internal Matrix4 Projection { get; private set; } = Matrix4.Identity;
        #endregion

        #region コンストラクタ
        internal Camera()
        {
            SetProjection(_fovy, _far, _aspect);
            SetMatrix(_position, _direction, _up);
        }
        #endregion

        /// <summary>描画先のサイズを変更します。<see cref="Projection"/> が変更されます</summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        internal void ChangeScreenSize(int width, int height)
        {
            ArgumentChecker.ThrowIf(width < 0, new ArgumentOutOfRangeException(nameof(width), width, "value is negative."));
            ArgumentChecker.ThrowIf(height < 0, new ArgumentOutOfRangeException(nameof(height), height, "value is negative"));
            _aspect = (float)width / height;
            SetProjection(_fovy, _far, _aspect);
        }

        #region private Method
        private void SetMatrix(Vector3 pos, Vector3 dir, Vector3 up)
        {
            View = Matrix4.LookAt(pos, pos + dir, up);
        }

        private void SetProjection(float radian, float far, float aspect)
        {
            Projection = Matrix4.CreatePerspectiveFieldOfView(radian, aspect, NEAR, far);
        }
        #endregion
    }
}
