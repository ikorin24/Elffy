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

        public CameraMouse(Camera camera, Mouse mouse, in Vector3 target)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _mouse = mouse ?? throw new ArgumentNullException(nameof(mouse));
            Target = target;
        }

        protected override void OnEarlyUpdate()
        {
            base.OnEarlyUpdate();
            var wheel = _mouse.Wheel();
            ZoomCamera(wheel);

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

            // target を原点とする座標系
            var v = _camera.Position - target;
            var alpha = -MathF.Atan2(v.Z, v.X);         // x軸正方向を0度とした時の現在の経度
            var beta = MathF.Atan2(v.Y, v.Xz.Length);   // 現在の緯度

            var maxBeta = 80.ToRadian();
            var minBeta = -maxBeta;

            // 緯度方向の回転角
            phiRad = MathF.Max(MathF.Min(phiRad, maxBeta - beta), minBeta - beta);

            // 経度を0に戻す → 緯度方向回転 → 元の経度+緯度回転
            var q1 = Quaternion.FromAxisAngle(Vector3.UnitY, -alpha);
            var q2 = Quaternion.FromAxisAngle(Vector3.UnitZ, phiRad);
            var q3 = Quaternion.FromAxisAngle(Vector3.UnitY, alpha + thetaRad);

            _camera.Position = q3 * q2 * q1 * v + target;
            _camera.LookAt(target);
        }

        private void ZoomCamera(float delta)
        {
            var ratio = 1 + delta;
            var vec = _camera.Position - Target;
            _camera.Position = _camera.Position + vec * delta * -0.1f;
            _camera.LookAt(Target);
        }
    }
}
