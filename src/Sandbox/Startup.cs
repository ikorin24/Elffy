#nullable enable
using System;
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
            screen.Timings.OnUpdate(() =>
            {
                if(screen.Keyboard.IsPress(Elffy.InputSystem.Keys.Escape)) {
                    screen.Close();
                }
            });
            var (drLayer, wLayer, uiLayer) =
                await LayerPipelines.CreateBuilder(screen).Build(
                    () => new DeferredRenderingLayer(),
                    () => new WorldLayer(),
                    () => new UILayer());

            await InitializeLights(wLayer);
            var uiRoot = uiLayer.UIRoot;
            var update = screen.Timings.Update;
            uiRoot.Background = Color4.Black;
            try {
                await ParallelOperation.WhenAll(
                    //Sample.CreateUI(uiLayer.UIRoot),
                    CreateDice2(drLayer),
                    CreateCameraMouse(wLayer, new Vector3(0, 3, 0)),
                    CreateDice(wLayer),
                    CreateModel2(wLayer),
                    CreateBox(drLayer),
                    CreateFloor(drLayer),
                    //CreateSky(wLayer),
                    new Sphere()
                    {
                        Position = new Vector3(-5, 1, 1),
                        Shader = new PbrDeferredShader()
                        {
                            Metallic = 1f,
                            BaseColor = new Color3(1f, 0.766f, 0.336f),
                            Roughness = 0.01f,
                        },
                    }.Activate(drLayer),
                    update.DelayTime(800)
                    );
                var time = TimeSpanF.FromMilliseconds(200);
                await foreach(var frame in update.Frames()) {
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

        private static async UniTask InitializeLights(WorldLayer layer)
        {
            var screen = layer.GetValidScreen();
            var pos = new Vector3(0, 10, 10);

            var color = Color4.White;
            var (
                arrow,
                sphere,
                light
                ) = await UniTask.WhenAll(
                new Arrow
                {
                    Shader = new SolidColorShader
                    {
                        Color = color,
                    },
                    HasShadow = false,
                    Scale = new Vector3(2)
                }.Activate(layer),
                new Sphere
                {
                    Shader = new SolidColorShader
                    {
                        Color = color,
                    },
                    Position = pos,
                    HasShadow = false,
                }.Activate(layer),
                //PointLight.Create(screen, pos, color)
                DirectLight.Create(screen, -pos, color)
                );

            var i = 0;
            screen.Timings.OnUpdate(() =>
            {
                var angle = i++.ToRadian();
                var (sin, cos) = MathF.SinCos(angle);
                sphere.Position = new Vector3(sin * 2, 10, cos * 10);

                //light.Position = sphere.Position;
                light.Direction = -sphere.Position;
                arrow.SetDirection(-sphere.Position.Normalized(), sphere.Position);
            });

            UniTask.Void(async () =>
            {
                var update = screen.Timings.Update;
                while(true) {
                    await update.Next();
                    if(screen.Keyboard.IsDown(Elffy.InputSystem.Keys.Space)) {
                        await light.Terminate();
                        return;
                    }
                }
            });
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
                Metallic = 0f,
                Roughness = 0.02f,
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
            var timing = layer.GetValidScreen().Timings.Update;
            var plain = new Plain();
            plain.Scale = new Vector3(40f);
            plain.Shader = new PhongShader();
            var config = new TextureConfig(TextureExpansionMode.NearestNeighbor, TextureShrinkMode.NearestNeighbor,
                                           TextureMipmapMode.None, TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);
            var texture = await Resources.Sandbox["floor.png"].LoadTextureAsync(config, timing);
            plain.AddComponent(texture);
            plain.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian());
            return await plain.Activate(layer);
        }

        private static async UniTask<Plain> CreateFloor(DeferredRenderingLayer layer)
        {
            var timing = layer.GetValidScreen().Timings.Update;
            var dice = new Plain()
            {
                Scale = new Vector3(150f),
                Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian()),
                Shader = new PbrDeferredShader()
                {
                    Metallic = 0,
                    Roughness = 0.25f,
                },
            };
            var texture = await Resources.Sandbox["floor.png"]
                .LoadTextureAsync(TextureConfig.DefaultNearestNeighbor, timing);
            dice.AddComponent(texture);
            return await dice.Activate(layer);
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

        private static async UniTask<Cube> CreateBox(DeferredRenderingLayer layer)
        {
            var timing = layer.GetValidScreen().Timings.Update;
            var cube = new Cube();
            cube.Position = new(-3, 0.5f, 0);
            cube.Shader = new PbrDeferredShader
            {
                Metallic = 0f,
                Roughness = 0.15f,
            };
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
