#nullable enable
using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Elffy;
using Elffy.Mathematics;
using Elffy.Shapes;
using Elffy.Shading.Deferred;
using Elffy.Shading.Forward;
using Elffy.Components;
using Elffy.UI;
using Elffy.Shading;
using Elffy.Threading;
using Elffy.Graphics.OpenGL;
using Elffy.Imaging;

namespace Sandbox
{
    public static class Startup
    {
        private static void Main(string[] args)
        {
            new AppStarter
            {
                Width = (int)(1200 * 1.5),
                Height = (int)(675 * 1.5f),
                Title = "Sandbox",
                AllowMultiLaunch = false,
                Style = WindowStyle.Default,
                Icon = Resources.Sandbox["icon.ico"],
                IsDebugMode = AssemblyBuildInfo.IsDebug,
            }.Run(Start);
        }

        private static async UniTask Start(IHostScreen screen)
        {
            UniTask.Void(async () =>
            {
                await foreach(var _ in screen.Frames(FrameTiming.Update)) {
                    if(screen.Keyboard.IsPress(Elffy.InputSystem.Keys.Escape)) {
                        screen.Close();
                    }
                }
            });
            var (drLayer, wLayer, uiLayer) =
                await LayerPipelines.CreateBuilder(screen).Build(
                    () => new DeferredRenderingLayer(),
                    () => new WorldLayer(),
                    () => new UILayer());

            InitializeLights(screen);
            var uiRoot = uiLayer.UIRoot;
            var timings = screen.TimingPoints;
            uiRoot.Background = Color4.Black;
            try {
                await ParallelOperation.WhenAll(
                    Sample.CreateUI(uiLayer.UIRoot),
                    CreateDice2(drLayer),
                    CreateCameraMouse(wLayer, new Vector3(0, 3, 0)),
                    CreateDice(wLayer),
                    CreateModel2(wLayer),
                    CreateBox(wLayer),
                    CreateFloor(wLayer),
                    CreateFloor2(wLayer),
                    CreateSky(wLayer),
                    timings.Update.DelayTime(800));
                var time = TimeSpanF.FromMilliseconds(200);
                await foreach(var frame in timings.Update.Frames()) {
                    if(frame.Time >= time) {
                        break;
                    }
                    uiRoot.Background.A = 1f - frame.Time / time;
                }
            }
            finally {
                uiRoot.Background = Color4.Transparent;
            }
        }

        private static void InitializeLights(IHostScreen screen)
        {
            ReadOnlySpan<Vector4> pos = stackalloc Vector4[]
            {
                new(1, 1, 0, 0),
                //new(1, 0, 0, 0),
            };
            ReadOnlySpan<Color4> color = stackalloc Color4[]
            {
                Color4.White,
                //Color4.OrangeRed,
            };
            screen.Lights.Initialize(pos, color);
        }

        private static UniTask<Model3D> CreateDice(WorldLayer layer)
        {
            var dice = Resources.Sandbox["Dice.fbx"].CreateFbxModel();
            dice.AddComponent(Resources.Sandbox["Dice.png"].LoadTexture());
            dice.Shader = new PhongShader();
            dice.Position = new Vector3(3, 1, -2);
            return dice.Activate(layer);
        }

        private static UniTask<Model3D> CreateDice2(DeferredRenderingLayer layer)
        {
            var dice = Resources.Sandbox["Dice.fbx"].CreateFbxModel();
            dice.AddComponent(Resources.Sandbox["Dice.png"].LoadTexture());
            dice.Position = new Vector3(3, 1, 2);
            dice.Shader = new PbrDeferredShader()
            {
                Metallic = 0.1f,
                Roughness = 0.05f,
            };
            return dice.Activate(layer);
        }

        private static async UniTask<Model3D> CreateModel2(WorldLayer layer)
        {
            var model = Resources.Sandbox["Alicia/Alicia_solid.pmx"].CreatePmxModel();
            model.Scale = new Vector3(0.3f);
            await model.Activate(layer);
            model.StartCoroutine(async (c, model) =>
            {
                while(c.CanRun) {
                    var keyborad = c.Screen.Keyboard;
                    if(keyborad.IsPress(Elffy.InputSystem.Keys.Down)) {
                        model.Position.Y -= 0.1f;
                    }
                    if(keyborad.IsPress(Elffy.InputSystem.Keys.Up)) {
                        model.Position.Y += 0.1f;
                    }
                    await c.Update.Next();
                }
            }).Forget();
            return model;
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
            var timing = layer.GetValidScreen().TimingPoints.Update;
            var plain = new Plain();
            plain.Scale = new Vector3(10f);
            plain.Shader = new PhongShader();
            var config = new TextureConfig(TextureExpansionMode.NearestNeighbor, TextureShrinkMode.NearestNeighbor,
                                           TextureMipmapMode.None, TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);
            var texture = await Resources.Sandbox["floor.png"].LoadTextureAsync(config, timing);
            plain.AddComponent(texture);
            plain.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian());
            return await plain.Activate(layer);
        }

        private static async UniTask<Plain> CreateFloor2(WorldLayer layer)
        {
            var bufSize = new Vector2i(256, 256);
            var buffer = ShaderStorageBuffer.CreateUninitialized<Color4>(bufSize.X * bufSize.Y);
            var dispatcher = new TestComputeShader(() => buffer.Ssbo).CreateDispatcher();
            var plain = new Plain();
            plain.Position.Z = -10;
            plain.Dead += _ =>
            {
                buffer.Dispose();
                dispatcher.Dispose();
            };
            plain.Updated += _ => dispatcher.Dispatch(bufSize.X, bufSize.Y, 1);
            plain.Scale = new Vector3(10f);
            plain.Shader = new TestShader(() => (buffer.Ssbo, bufSize.X, bufSize.Y));
            plain.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian());

            return await plain.Activate(layer);
        }

        private static async UniTask<Cube> CreateBox(WorldLayer layer)
        {
            var timing = layer.GetValidScreen().TimingPoints.Update;
            var cube = new Cube();
            cube.Position = new(-3, 0.5f, 0);
            cube.Shader = new PhongShader();
            cube.AddComponent(await Resources.Sandbox["box.png"].LoadTextureAsync(timing));
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
