#nullable enable
using System;
using Cysharp.Threading.Tasks;
using Elffy;
using Elffy.Mathematics;
using Elffy.Shapes;
using Elffy.Shading.Defered;
using Elffy.Shading.Forward;
using Elffy.Components;
using Elffy.Effective;
using Elffy.UI;

namespace Sandbox
{
    public static class Startup
    {
        [GameEntryPoint]
        public static async UniTask Start2()
        {
            var screen = Game.Screen;
            var layer = await WorldLayer.NewActivate(screen, "Default");

            var deferedRenderer = DeferedRenderer.Attach(screen, 1, context =>
            {
                var interval = 100;
                for(int i = 0; i < context.LightCount; i++) {
                    var x = i * interval - context.LightCount * interval / 2;
                    var pos = new Vector3(x, 10, 0);
                    context.SetPointLight(i, pos, Color4.White);
                }
            });

            var cube = Resources.Sandbox["Dice.fbx"].CreateFbxModel();
            var material = new PBRMaterialData(new Color3(1, 0.8f, 0.2f), 0.99f, 0.1f, default).ToMaterial();
            cube.AddComponent(material);
            cube.Shader = PBRDeferedShader.Instance;
            await UniTask.WhenAll(cube.Activate(layer), CreateCameraMouse(layer, cube.Position));

            //Game.Screen.StartCoroutine(deferedRenderer, static async (coroutine, deferedRenderer) =>
            //{
            //    using var pos = new ValueTypeRentMemory<Vector4>(deferedRenderer.LightCount);
            //    using var color = new ValueTypeRentMemory<Color4>(deferedRenderer.LightCount);
            //    long i = 0;
            //    while(coroutine.CanRun) {
            //        await Timing.DelayTime(1000);
            //        color[0] = Rand.Color4();
            //        if(i % 2 == 0) {
            //            pos[0] = new Vector4(0, 100, -50, 1f);
            //        }
            //        else {
            //            pos[0] = new Vector4(0, 100, 50, 1f);
            //        }
            //        deferedRenderer.UpdateLightPositions(pos.AsSpan());
            //        deferedRenderer.UpdateLightColors(color.AsSpan());
            //        i++;
            //    }
            //}).Forget();
        }

        //[GameEntryPoint]
        public static async UniTask Start()
        {
            var screen = Game.Screen;
            var timings = screen.TimingPoints;

            var (layer, uiLayer) = await UniTask.WhenAll(
                WorldLayer.NewActivate(screen, "Default"),
                UILayer.NewActivate(screen, "UI"));
            var uiRoot = uiLayer.UIRoot;
            uiRoot.Background = Color4.Black;
            try {
                await UniTask.WhenAll(
                    CreateModel1(layer),
                    CreateModel2(layer),
                    CreateBox(layer),
                    CreateFloor(layer),
                    CreateSky(layer),
                    CreateCameraMouse(layer, new Vector3(0, 3, 0)),
                    timings.Update.DelayTime(800));
                var time = TimeSpan.FromMilliseconds(200);
                await foreach(var frame in timings.Update.Frames()) {
                    if(frame.Time >= time) {
                        break;
                    }
                    uiRoot.Background.A = 1f - (float)frame.Time.Ticks / time.Ticks;
                }
            }
            finally {
                uiRoot.Background = Color4.Transparent;
            }
        }

        private static UniTask<Model3D> CreateModel1(WorldLayer layer)
        {
            var dice = Resources.Sandbox["Dice.fbx"].CreateFbxModel();
            dice.Position.X = 3f;
            dice.Position.Y = 1.5f;
            return dice.Activate(layer);
        }

        private static UniTask<Model3D> CreateModel2(WorldLayer layer)
        {
            var model = Resources.Sandbox["Alicia/Alicia_solid.pmx"].CreatePmxModel();
            model.Scale = new Vector3(0.3f);
            return model.Activate(layer);
        }

        private static UniTask<SkySphere> CreateSky(WorldLayer layer)
        {
            var sky = new SkySphere();
            sky.Shader = SkyShader.Instance;
            sky.Scale = new Vector3(500f);
            return sky.Activate(layer);
        }

        private static async UniTask<Plain> CreateFloor(WorldLayer layer)
        {
            var plain = new Plain();
            plain.Scale = new Vector3(10f);
            plain.Shader = PhongShader.Instance;
            var config = new TextureConfig(TextureExpansionMode.NearestNeighbor, TextureShrinkMode.NearestNeighbor,
                                           TextureMipmapMode.None, TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);
            var texture = await Resources.Sandbox["floor.png"].LoadTextureAsync(config);
            plain.AddComponent(texture);
            plain.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian());
            return await plain.Activate(layer);
        }

        private static async UniTask<Cube> CreateBox(WorldLayer layer)
        {
            var cube = new Cube();
            cube.Position = new(-3, 0.5f, 0);
            cube.Shader = PhongShader.Instance;
            cube.AddComponent(await Resources.Sandbox["box.png"].LoadTextureAsync());
            await cube.Activate(layer);
            cube.StartCoroutine(static async (coroutine, cube) =>
            {
                while(coroutine.CanRun) {
                    await coroutine.Update.Next();
                    cube.Rotate(Vector3.UnitY, 1f.ToRadian());
                }
            }).Forget();
            return cube;
        }

        private static UniTask<FrameObject> CreateCameraMouse(WorldLayer layer, Vector3 target)
        {
            var initialCameraPos = target + new Vector3(0, 1.5f, 20);
            return CameraMouse.Activate(layer, target, initialCameraPos);
        }
    }
}
