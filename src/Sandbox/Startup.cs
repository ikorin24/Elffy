#nullable enable
using System;
using Elffy;
using Elffy.Mathematics;
using Elffy.Shapes;
using Elffy.Shading;
using Cysharp.Threading.Tasks;
using System.Diagnostics;
using Elffy.Components;

namespace Sandbox
{
    public static class Startup
    {
        [GameEntryPoint]
        public static async UniTask Start()
        {
            GameUI.Root.Background = Color4.Black;
            try {
                await UniTask.WhenAll(
                    CreateModel1(),
                    CreateModel2(),
                    CreateBox(),
                    CreateCameraMouse(),
                    CreateFloor(),
                    CreateSky(),
                    Timing.DelayTime(800));

                await Timing.Ensure(FrameLoopTiming.Update);

                var time = TimeSpan.FromMilliseconds(200);
                await foreach(var frame in Timing.Frames()) {
                    if(frame.Time >= time) {
                        break;
                    }
                    GameUI.Root.Background.A = 1f - (float)frame.Time.Ticks / time.Ticks;
                }
            }
            finally {
                GameUI.Root.Background = Color4.Transparent;
            }
        }

        private static UniTask<Model3D> CreateModel1()
        {
            var dice = Resources.Loader.CreateFbxModel("Dice.fbx");
            dice.Position.X = 3f;
            dice.Position.Y = 1.5f;
            return dice.Activate();
        }

        private static UniTask<Model3D> CreateModel2()
        {
            var model = Resources.Loader.CreatePmxModel("Alicia/Alicia_solid.pmx");
            model.Scale = new Vector3(0.3f);
            return model.Activate();
        }

        private static UniTask<SkySphere> CreateSky()
        {
            var sky = new SkySphere();
            sky.Shader = SkyShaderSource.Instance;
            sky.Scale = new Vector3(500f);
            return sky.Activate();
        }

        private static async UniTask<Plain> CreateFloor()
        {
            var plain = new Plain();
            plain.Scale = new Vector3(10f);
            plain.Shader = PhongShaderSource.Instance;
            var config = new TextureConfig(TextureExpansionMode.NearestNeighbor, TextureShrinkMode.NearestNeighbor,
                                           TextureMipmapMode.None, TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);
            var texture = await Resources.Loader.LoadTextureAsync("floor.png", config);
            plain.AddComponent(texture);
            plain.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian());
            return await plain.Activate();
        }

        private static async UniTask<Cube> CreateBox()
        {
            var cube = new Cube();
            cube.Position = new(-3, 0.5f, 0);
            cube.Shader = PhongShaderSource.Instance;
            cube.AddComponent(await Resources.Loader.LoadTextureAsync("box.png"));
            await cube.Activate();
            StartMotion(cube).Forget();
            return cube;

            static async UniTaskVoid StartMotion(Cube cube)
            {
                while(Game.IsRunning) {
                    cube.Rotate(Vector3.UnitY, 1f.ToRadian());
                    await Timing.ToUpdate();
                }
            }
        }

        private static UniTask<CameraMouse> CreateCameraMouse()
        {
            var cameraTarget = new Vector3(0, 3, 0);
            Game.Camera.LookAt(cameraTarget, new Vector3(0, 4.5f, 20));
            var cameraMouse = new CameraMouse(Game.Camera, Game.Mouse, cameraTarget);
            return cameraMouse.Activate();
        }
    }
}
