#nullable enable
using System;
using Elffy.InputSystem;
using Elffy.Mathmatics;

namespace Elffy
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

        private void MoveCamera(float thetaRad, float phiRad)
        {
            var target = Target;
            var p0 = _camera.Position - target;
            var theta = new Angle(thetaRad);                // 水平方向の回転角
            var phi = new Angle(phiRad);                    // 垂直方向の回転角
            var alpha = new Angle(p0.X / p0.Xz.Length, p0.Z / p0.Xz.Length);    // XZ平面への正射影ベクトルとZ軸との角 (経度)

            // 経度0まで
            var matrix = 
            // 垂直方向回転 (経度0へ水平方向回転 → 垂直方向回転 → 元の経度まで水平方向回転)
                new Matrix3(alpha.Cos, 0, alpha.Sin,
                            0, 1, 0,
                            -alpha.Sin, 0, alpha.Cos)
               * new Matrix3(1, 0, 0,
                             0, phi.Cos, -phi.Sin,
                             0, phi.Sin, phi.Cos)
               * new Matrix3(alpha.Cos, 0, -alpha.Sin,
                             0, 1, 0,
                             alpha.Sin, 0, alpha.Cos)
            // 水平方向回転
               * new Matrix3(theta.Cos, 0, -theta.Sin,
                             0, 1, 0,
                             theta.Sin, 0, theta.Cos);

            _camera.Direction = _camera.Direction * matrix;
            _camera.Position = target + p0 * matrix;
        }

        private readonly struct Angle
        {
            public readonly float Sin;
            public readonly float Cos;

            public Angle(float theta)
            {
                Sin = MathTool.Sin(theta);
                Cos = MathTool.Cos(theta);
            }

            public Angle(float sin, float cos)
            {
                Sin = sin;
                Cos = cos;
            }
        }
    }
}
