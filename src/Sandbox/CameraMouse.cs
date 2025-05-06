#nullable enable
using System;
using System.Diagnostics;
using Elffy;
using Elffy.InputSystem;
using Elffy.Mathematics;

namespace Sandbox
{
    public static class CameraMouse
    {
        public static void Attach(IHostScreen screen, Vector3 initialCameraPos, Vector3 initialDir)
        {
            var mouse = screen.Mouse;
            var keyboard = screen.Keyboard;
            var camera = screen.Camera;
            var target = initialCameraPos + initialDir.Normalized() * 20f;
            camera.LookAt(target, initialCameraPos);

            var currentTarget = target;
            screen.Timings.EarlyUpdate.Subscribe(_ =>
            {
                var cameraPos = camera.Position;
                var posChanged = false;

                if(keyboard.IsPress(Keys.W) || keyboard.IsPress(Keys.A) || keyboard.IsPress(Keys.S) || keyboard.IsPress(Keys.D)
                || keyboard.IsPress(Keys.E) || keyboard.IsPress(Keys.Q)) {
                    const float S = 0.3f;
                    var v = camera.Direction * S;
                    Vector3 vec = default;
                    if(keyboard.IsPress(Keys.W)) {
                        vec += v;
                    }
                    if(keyboard.IsPress(Keys.S)) {
                        vec -= v;
                    }
                    if(keyboard.IsPress(Keys.A)) {
                        var left = Matrix2.GetRotation(-90.ToRadian()) * v.Xz;
                        vec += new Vector3(left.X, 0, left.Y);
                    }
                    if(keyboard.IsPress(Keys.D)) {
                        var right = Matrix2.GetRotation(90.ToRadian()) * v.Xz;
                        vec += new Vector3(right.X, 0, right.Y);
                    }
                    if(keyboard.IsPress(Keys.E)) {
                        vec += Vector3.UnitY * S;
                    }
                    if(keyboard.IsPress(Keys.Q)) {
                        vec -= Vector3.UnitY * S;
                    }
                    currentTarget += vec;
                    cameraPos += vec;
                    posChanged = true;
                }
                if(mouse.IsLeftPressed()) {
                    var vec = mouse.PositionDelta * (MathTool.PiOver180 * 0.1f);
                    cameraPos = CalcCameraPosition(cameraPos, currentTarget, vec.X, vec.Y);
                    posChanged = true;
                }

                //var wheelDelta = mouse.WheelDelta;
                //if(wheelDelta != 0) {
                //    cameraPos += (cameraPos - currentTarget) * wheelDelta * -0.1f;
                //    posChanged = true;
                //}

                if(posChanged) {
                    camera.LookAt(currentTarget, cameraPos);
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
