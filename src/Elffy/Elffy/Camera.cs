using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy.Core;
using OpenTK;

namespace Elffy
{
    public class Camera
    {
        private const float NEAR = 0.3f;

        #region Current
        public static Camera Current
        {
            get => _current;
            set { _current = value ?? throw new ArgumentNullException(); }
        }
        private static Camera _current = new Camera();
        #endregion

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
                if(value.X == 0 && value.Y == 0 && value.Z == 0) { throw new ArgumentException("Value must be non-Zero vector."); }
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
                if(value.X == 0 && value.Y == 0 && value.Z == 0) { throw new ArgumentException("Value must be non-Zero vector."); }
                _up = value;
                SetMatrix(_position, _direction, _up);
            }
        }
        private Vector3 _up = Vector3.UnitY;
        #endregion

        #region Fovy
        /// <summary>Y方向視野角(度)[0 ~ 180]</summary>
        public float Fovy
        {
            get { return _fovy; }
            set
            {
                _fovy = value;
                var radian = _fovy / 180f * (float)Math.PI;
                if(radian <= 0 || radian > Math.PI) { throw new ArgumentException("Value must be 0 ~ 180. (not include 0)"); }
                SetProjection(radian, _far);
            }
        }
        private float _fovy = 25;
        #endregion

        #region Far
        public float Far
        {
            get { return _far; }
            set
            {
                if(value <= NEAR) { throw new ArgumentException("Value must be bigger than 0. (or value is too small.)"); }
                _far = value;
                var radian = _fovy / 180f * (float)Math.PI;
                SetProjection(radian, _far);
            }
        }
        private float _far = 2000f;
        #endregion

        #region internal Property
        internal Matrix4 Matrix { get; private set; } = Matrix4.Identity;

        internal Matrix4 Projection { get; private set; } = Matrix4.Identity;
        #endregion

        #region コンストラクタ
        public Camera()
        {
            SetProjection(_fovy / 180f * (float)Math.PI, _far);
            SetMatrix(_position, _direction, _up);
        }
        #endregion

        #region private Method
        private void SetMatrix(Vector3 pos, Vector3 dir, Vector3 up)
        {
            Matrix = Matrix4.LookAt(pos, pos + dir, up);
        }

        private void SetProjection(float radian, float far)
        {
            Projection = Matrix4.CreatePerspectiveFieldOfView(radian, (float)Game.ClientSize.Width / Game.ClientSize.Height, NEAR, far);
        }
        #endregion
    }
}
