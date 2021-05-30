#nullable enable
using System;
using Elffy;
using Elffy.Mathematics;
using Elffy.Shapes;
using Elffy.Shading;
using Cysharp.Threading.Tasks;

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
                    CreateModel(),
                    CreateBox(),
                    UniTask.FromResult(CameraMouse()),
                    CreateFloor(),
                    UniTask.FromResult(CreateSky()),
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

        private static async UniTask<Model3D> CreateModel()
        {
            //var model = Resources.Loader.CreatePmxModel("Alicia/Alicia_solid.pmx");
            //model.Scale = new Vector3(0.3f);
            //await model.ActivateWaitLoaded();
            //UniTask.Create(async () =>
            //{
            //    var humanoid = model.GetComponent<Elffy.Components.HumanoidSkeleton>();
            //    await foreach(var frame in Timing.Frames()) {
            //        using var handler = humanoid.StartTranslation();
            //        handler.MoveBoneCCDIK(11, Vector3.UnitY * 10, 3);
            //    }
            //}).Forget();

            //return model;


            var dice = Resources.Loader.CreateFbxModel("Hidden/julia.fbx");
            await dice.ActivateWaitLoaded();
            dice.Position.X = 1;
            dice.Position.Y = 0;
            dice.Scale = new Vector3(0.05f);
            return dice;
        }

        private static SkySphere CreateSky()
        {
            var sky = new SkySphere();
            sky.Shader = SkyShaderSource.Instance;
            sky.Scale = new Vector3(500f);
            sky.Activate();
            return sky;
        }

        private static async UniTask<Plain> CreateFloor()
        {
            var plain = new Plain();
            plain.Scale = new Vector3(10f);
            plain.Shader = PhongShaderSource.Instance;
            var texture = await Resources.Loader.LoadTextureAsync("floor.png", TextureExpansionMode.NearestNeighbor, TextureShrinkMode.NearestNeighbor,
                                TextureMipmapMode.None, TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);
            plain.AddComponent(texture);
            plain.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian());
            plain.Activate();
            return plain;
        }

        private static async UniTask<Cube> CreateBox()
        {
            var cube = new Cube();
            cube.Position = new(-3, 0.5f, 0);
            cube.Shader = PhongShaderSource.Instance;
            cube.AddComponent(await Resources.Loader.LoadTextureAsync("box.png"));
            cube.Activate();
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
