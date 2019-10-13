using Elffy;
using Elffy.Framing;
using Elffy.Mathmatics;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElffyGame
{
    public static class MainCamera
    {
        private static bool _isInit;
        private static Camera _camera;

        static MainCamera()
        {
            _camera = Game.Camera;
        }

        public static void Init()
        {
            if(_isInit) { return; }
            _isInit = true;

            FrameProcess.While(() => true, _ => {
                if(Controller.ButtonA()) {
                    Rotation();
                } else {
                    Move();
                }
            });
        }

        private static void Move()
        {
            var axis = Controller.Axis();
            if(axis == Vector2.Zero) { return; }

            var speed = Game.RenderDelta / 1000 * 3f * axis.Normalized();
            var direction = _camera.Direction.Normalized();
            _camera.Position += direction * speed.Y;
            var rot = Matrix3.CreateFromAxisAngle(Vector3.UnitY, -MathHelper.PiOver2);
            var rightLight = (direction * rot).Xz.Normalized() * speed.X;
            _camera.Position += new Vector3(rightLight.X, 0, rightLight.Y);
        }

        private static void Rotation()
        {
            var axis = Controller.Axis();
            if(axis == Vector2.Zero) { return; }
            var theta = axis.X * MathHelper.DegreesToRadians(90f) * Game.RenderDelta / 1000;
            _camera.Direction *= Matrix3.CreateFromAxisAngle(Vector3.UnitY, -theta);
            var fai = axis.Y * MathHelper.DegreesToRadians(90f) * Game.RenderDelta / 1000;
            var rot = Matrix3.CreateFromAxisAngle(Vector3.UnitY, -MathHelper.PiOver2);
            var tmp = (_camera.Direction * rot).Xz;
            _camera.Direction *= Matrix3.CreateFromAxisAngle(new Vector3(tmp.X, 0, tmp.Y), -fai);
        }
    }
}
