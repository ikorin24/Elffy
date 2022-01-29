#nullable enable
using System;
using Cysharp.Threading.Tasks;
using Elffy;
using Elffy.InputSystem;
using Elffy.Mathematics;

namespace Sandbox
{
    public static class CameraMouse
    {
        private static readonly FrameTiming LoopTiming = FrameTiming.EarlyUpdate;
        private static readonly Func<CoroutineState, (Vector3, Vector3), UniTask> MoveCameraCoroutineFunc = MoveCameraCoroutine;

        private sealed class CameraMouseObject : FrameObject
        {
        }

        public static async UniTask<FrameObject> Activate(WorldLayer layer, Vector3 target, Vector3 initialCameraPos)
        {
            var obj = new CameraMouseObject();
            await obj.Activate(layer);
            obj.StartCoroutine((target, initialCameraPos), MoveCameraCoroutineFunc, LoopTiming).Forget();
            return obj;
        }

        public static void Attach(FrameObject parent, in Vector3 target, in Vector3 initialCameraPos)
        {
            parent.StartCoroutine((target, initialCameraPos), MoveCameraCoroutineFunc, LoopTiming).Forget();
        }

        public static void Attach(IHostScreen parent, in Vector3 target, in Vector3 initialCameraPos)
        {
            parent.StartCoroutine((target, initialCameraPos), MoveCameraCoroutineFunc, LoopTiming).Forget();
        }

        private static async UniTask MoveCameraCoroutine(CoroutineState coroutine, (Vector3 Target, Vector3 InitialPos) args)
        {
            var (target, initialPos) = args;
            var screen = coroutine.Screen;
            var mouse = screen.Mouse;
            var camera = screen.Camera;
            var timingPoint = coroutine.TimingOf(LoopTiming);

            camera.LookAt(target, initialPos);
            while(coroutine.CanRun) {
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
                await timingPoint.Next();
            }
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
