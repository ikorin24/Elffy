#nullable enable
using System;
using Elffy;
using Elffy.InputSystem;
using Elffy.Mathematics;

namespace Sandbox
{
    public class CameraMouse : FrameObject
    {
        private Camera _camera;
        private Mouse _mouse;
        private Vector2 _mousePos;
        private bool _isMousePressed;
        private float _sensitivity = 0.01f;

        public Vector3 Target { get; set; }

        public CameraMouse(Camera camera, Mouse mouse, Vector3 target)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _mouse = mouse ?? throw new ArgumentNullException(nameof(mouse));
            Target = target;
            EarlyUpdated += OnEarlyUpdated;
        }

        private void OnEarlyUpdated(FrameObject sender)
        {
            if(_mouse.IsDown(MouseButton.Left)) {
                _mousePos = _mouse.Position;
                _isMousePressed = true;
            }
            if(_mouse.IsUp(MouseButton.Left)) {
                _mousePos = default;
                _isMousePressed = false;
            }
            if(!_isMousePressed) { return; }

            var vec = (_mouse.Position - _mousePos) * _sensitivity;  // マウス移動差分
            MoveCamera(-vec.X, vec.Y);
            _mousePos = _mouse.Position;
        }

        /// <summary><see cref="Target"/>を中心にカメラを回転させます</summary>
        /// <param name="thetaRad">水平方向(経度)回転角[rad]</param>
        /// <param name="phiRad">垂直方向(緯度)回転角[rad]</param>
        private void MoveCamera(float thetaRad, float phiRad)
        {
            var target = Target;
            var p0 = _camera.Position - target;
            var theta = new Angle(thetaRad);                // 水平方向の回転角
            var phi = new Angle(phiRad);                    // 垂直方向の回転角
            var alpha = new Angle(p0.X / p0.Xz.Length, p0.Z / p0.Xz.Length);    // XZ平面への正射影ベクトルとZ軸との角 (経度)

            var matrix = new Matrix3(theta.Cos, 0, theta.Sin,   // 水平方向回転
                                     0, 1, 0,
                                     -theta.Sin, 0, theta.Cos)  //  ↑
                       * new Matrix3(alpha.Cos, 0, alpha.Sin,   // 元の経度へ
                                     0, 1, 0,
                                     -alpha.Sin, 0, alpha.Cos)  //  ↑
                       * new Matrix3(1, 0, 0,                   // 垂直方向回転
                                     0, phi.Cos, phi.Sin,
                                     0, -phi.Sin, phi.Cos)      //  ↑
                       * new Matrix3(alpha.Cos, 0, -alpha.Sin,  // 経度0 (Z軸正方向)へ水平方向回転
                                     0, 1, 0,
                                     alpha.Sin, 0, alpha.Cos);

            _camera.Direction = matrix * _camera.Direction;
            _camera.Position = target + matrix * p0;
        }

        private readonly struct Angle
        {
            public readonly float Sin;
            public readonly float Cos;

            public Angle(float theta)
            {
                Sin = MathF.Sin(theta);
                Cos = MathF.Cos(theta);
            }

            public Angle(float sin, float cos)
            {
                Sin = sin;
                Cos = cos;
            }
        }
    }
}
