#nullable enable
using System;
using Elffy;
using Elffy.Mathematics;
using Elffy.Shapes;
using Elffy.Imaging;
using Elffy.Shading;
using Cysharp.Threading.Tasks;

namespace Sandbox
{
    public static class Startup
    {
        public static async UniTask Start()
        {
            GameUI.Root.Background = Color4.Black;

            await UniTask.WhenAll(
                CreateBox(),
                UniTask.FromResult(CameraMouse()),
                UniTask.FromResult(CreatePlain()),
                UniTask.FromResult(CreateSky()),
                Timing.DelayTime(800));

            var time = TimeSpan.FromMilliseconds(200);
            await foreach(var frame in Timing.Frames.OnTiming(FrameLoopTiming.Update)) {
                if(frame.Time >= time) {
                    break;
                }
                GameUI.Root.Background.A = 1f - (float)frame.Time.Ticks / time.Ticks;
            }
            GameUI.Root.Background = Color4.Transparent;
        }

        private static SkySphere CreateSky()
        {
            var sky = new SkySphere();
            sky.Shader = SkyShaderSource.Instance;
            sky.Scale = new Vector3(500f);
            sky.Activate();
            return sky;
        }

        private static Plain CreatePlain()
        {
            var p = new Plain();
            p.Scale = new Vector3(100f);
            p.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian());
            p.Activate();
            return p;
        }

        private static async UniTask<Cube> CreateBox()
        {
            var cube = new Cube();
            cube.Position = new(0, 0.5f, 0);
            cube.Shader = PhongShaderSource.Instance;
            cube.AddComponent(await Resources.Loader.LoadTextureAsync("box.png", BitmapType.Png, FrameLoopTiming.Update));
            cube.Activate();
            return cube;
        }

        private static CameraMouse CameraMouse()
        {
            var cameraTarget = new Vector3(0, 3, 0);
            Game.Camera.LookAt(cameraTarget, new Vector3(0, 4.5f, 20));
            var cameraMouse = new CameraMouse(Game.Camera, Game.Mouse, cameraTarget);
            cameraMouse.Activate();
            return cameraMouse;
        }
    }
}
