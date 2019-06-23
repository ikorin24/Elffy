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
        private const int MAIN_CAMERA = 0;
        private const float NEAR = 0.1f;
        private static int _numGenerator = MAIN_CAMERA;
        private static readonly Dictionary<int, Camera> _cameras = new Dictionary<int, Camera>();

        public static Camera Current { get; private set; }

        public int Num { get; private set; }

        public Vector3 Position
        {
            get { return _position; }
            set
            {
                _position = value;
                SetMatrix(_position, _direction, _up);
            }
        }
        private Vector3 _position = Vector3.UnitZ;

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

        internal Matrix4 Matrix { get; private set; } = Matrix4.Identity;

        internal Matrix4 Projection { get; private set; } = Matrix4.Identity;

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
        private float _fovy = 45;
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
        private float _far = 100f;
        #endregion

        #region コンストラクタ
        static Camera()
        {
            var mainCamera = new Camera();
            _cameras.Add(mainCamera.Num, mainCamera);
            Current = mainCamera;
        }

        private Camera()
        {
            Num = _numGenerator++;
            SetProjection(_fovy / 180f * (float)Math.PI, _far);
            SetMatrix(_position, _direction, _up);
        }
        #endregion

        public static int GenerateCamera()
        {
            var newCamera = new Camera();
            _cameras.Add(newCamera.Num, newCamera);
            return newCamera.Num;
        }

        public static void Move(float x, float y, float z)
        {
            if(x == 0 && y == 0 && z == 0) { return; }
            Current.Position += new Vector3(x, y, z);
        }

        #region ChangeCamera
        /// <summary>番号を指定してカメラを変更します</summary>
        /// <param name="cameraNum">カメラ番号</param>
        public static void ChangeCamera(int cameraNum)
        {
            if(_cameras.TryGetValue(cameraNum, out var camera)) {
                Current = camera;
            }
            else { throw new ArgumentException($"The camera of number '{cameraNum}' does not exist."); }
        }
        #endregion

        #region GetCamera
        /// <summary>番号を指定してカメラを取得します</summary>
        /// <param name="cameraNum">カメラ番号</param>
        /// <returns>カメラ</returns>
        public static Camera GetCamera(int cameraNum)
        {
            if(_cameras.TryGetValue(cameraNum, out var camera)) {
                return camera;
            }
            else { throw new ArgumentException($"The camera of number '{cameraNum}' does not exist."); }
        }
        #endregion

        #region RemoveCamera
        /// <summary>番号を指定してカメラを削除します</summary>
        /// <param name="cameraNum">カメラ番号</param>
        public static void RemoveCamera(int cameraNum)
        {
            if(cameraNum == MAIN_CAMERA) { throw new ArgumentException("Main camera cannot be removed."); }
            _cameras.Remove(cameraNum);
        }
        #endregion

        private void SetMatrix(Vector3 pos, Vector3 dir, Vector3 up)
        {
            Matrix = Matrix4.LookAt(pos, pos + dir, up);
        }

        private void SetProjection(float radian, float far)
        {
            Projection = Matrix4.CreatePerspectiveFieldOfView(radian, Game.AspectRatio, NEAR, far);
        }
    }
}
