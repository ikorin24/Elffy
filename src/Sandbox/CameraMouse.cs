#nullable enable
using System;
using Elffy;
using Elffy.InputSystem;
using Elffy.Mathematics;

namespace Sandbox
{
    public static class CameraMouse
    {
        public static void Attach(IHostScreen screen, Vector3 target, Vector3 initialCameraPos)
        {
            var mouse = screen.Mouse;
            var camera = screen.Camera;
            camera.LookAt(target, initialCameraPos);
            screen.Timings.EarlyUpdate.Subscribe(_ =>
            {
                var cameraPos = camera.Position;
                var posChanged = false;
                if(mouse.IsLeftPressed()) {
                    var vec = mouse.PositionDelta * (MathTool.PiOver180 * 0.5f);
                    cameraPos = CalcCameraPosition(cameraPos, target, vec.X, vec.Y);
                    posChanged = true;
                }

                var wheelDelta = mouse.WheelDelta;
                if(wheelDelta != 0) {
                    cameraPos += (cameraPos - target) * wheelDelta * -0.1f;
                    posChanged = true;
                }

                if(posChanged) {
                    camera.LookAt(target, cameraPos);
                }
            });
        }

        private static Vector3 CalcCameraPosition(in Vector3 cameraPos, in Vector3 center, float horizontalAngle, float verticalAngle)
        {
            const float MaxVertical = 89.99f * MathTool.PiOver180;
            const float MinVertical = -MaxVertical;
            var vec = cameraPos - center;
            var radius = vec.Length;
            var xzLength = vec.Xz.Length;
            var beta = MathF.Atan2(vec.Y, xzLength) + verticalAngle;
            beta = MathF.Max(MathF.Min(beta, MaxVertical), MinVertical);

            Vector3 result;
            var (sinBeta, cosBeta) = MathF.SinCos(beta);
            (result.X, result.Z) = Matrix2.GetRotation(horizontalAngle) * vec.Xz * (radius * cosBeta / xzLength);
            result.Y = radius * sinBeta;
            return result + center;
        }
    }
}
