#nullable enable
using System;
using Cysharp.Threading.Tasks;
using Elffy;
using Elffy.Mathematics;
using Elffy.Shapes;
using Elffy.Shading.Deferred;
using Elffy.Shading.Forward;
using Elffy.UI;
using Elffy.Shading;
using Elffy.Threading;
using Elffy.Imaging;
using Elffy.Graphics.OpenGL;
using System.Diagnostics;

[assembly: DefineLocalResource("Sandbox", "Sandbox.dat")]

namespace Sandbox;

public static class Startup
{
    private static void Main(string[] args) =>
        AppStarter.Create(new()
        {
            Width = 1800,
            Height = 1012,
            Title = "Sandbox",
            AllowMultiLaunch = false,
            Style = WindowStyle.Default,
            Icon = Resources.Sandbox["icon.ico"],
            IsDebugMode = AssemblyBuildInfo.IsDebug,
        }).Run(Start);

    private static async UniTask<(DeferredRenderLayer, ForwardRenderLayer, UIObjectLayer)> CreateRenderPipeline(IHostScreen screen)
    {
        var offscreen = new OffscreenBuffer();
        offscreen.Initialize(screen);
        var deferred = new DeferredRenderLayer(offscreen.FBO, -100);
        var forward = new ForwardRenderLayer(offscreen.FBO, 0);
        var ppo = new PostProcessOperation(1000)
        {
            PostProcess = new GrayscalePostProcess(offscreen),
        };
        var ui = new UIObjectLayer(1001);
        ppo.SizeChanged.Subscribe(_ => offscreen.ResizeToScreenFrameBufferSize());
        ppo.AfterExecute.Subscribe(_ => offscreen.ClearAllBuffers());
        ppo.Dead.Subscribe(_ => offscreen.Dispose());
        await ParallelOperation.WhenAll(
            deferred.Activate(screen),
            forward.Activate(screen),
            ui.Activate(screen),
            ppo.Activate(screen));

        return (deferred, forward, ui);
    }

    private static async UniTask Start(IHostScreen screen)
    {
        screen.Timings.Update.Subscribe(_ =>
        {
            if(screen.Keyboard.IsPress(Elffy.InputSystem.Keys.Escape)) {
                screen.Close();
            }
        });
        var (deferred, forward, ui) = await CreateRenderPipeline(screen);
        var uiRoot = ui.UIRoot;
        var update = screen.Timings.Update;
        uiRoot.Background = Color4.Black;
        try {
            CameraMouse.Attach(screen, new Vector3(0, 3, 0), new Vector3(0, 6, 40));
            await ParallelOperation.WhenAll(
                InitializeLights(forward),
                new Gizmo().Activate(forward),
                Sample.CreateUI(ui.UIRoot),
                CreateDice2(deferred),
                CreateDice(forward),
                CreateDiceWireframe(forward),
                CreateModel2(forward),
                CreateBox(deferred),
                CreateFloor(deferred),
                CreateModel3(deferred),
                CreateSky(forward),
                new Sphere()
                {
                    Position = new Vector3(-5, 1, 1),
                    Shader = new PbrDeferredShader()
                    {
                        Metallic = 0f,
                        BaseColor = new Color3(1f, 0.766f, 0.336f),
                        Roughness = 0.01f,
                    },
                }.Activate(deferred),
                new Sphere()
                {
                    Position = new Vector3(-5, 1, 3),
                    Shader = new PbrDeferredShader()
                    {
                        Metallic = 1f,
                        BaseColor = new Color3(1f, 0.766f, 0.336f),
                        Roughness = 0.01f,
                    },
                }.Activate(deferred),
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

    private static async UniTask InitializeLights(ForwardRenderLayer layer)
    {
        var screen = layer.GetValidScreen();
        var pos = new Vector3(0, 10, 10);

        var color = Color4.White;
        var (
            //arrow,
            sphere,
            light
            ) = await UniTask.WhenAll(
            //new Arrow
            //{
            //    Shader = new SolidColorShader
            //    {
            //        Color = color,
            //    },
            //    HasShadow = false,
            //    Scale = new Vector3(2)
            //}.Activate(layer),
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
        screen.Timings.Update.Subscribe(_ =>
        {
            var angle = i++.ToRadian();
            var (sin, cos) = MathF.SinCos(angle);
            sphere.Position = new Vector3(sin * 2, 10, cos * 10);

            //light.Position = sphere.Position;
            light.Direction = -sphere.Position;
            //arrow.SetDirection(-sphere.Position.Normalized(), sphere.Position);
        });
    }

    private static async UniTask<Model3D> CreateDice(ForwardRenderLayer layer)
    {
        var timing = layer.GetValidScreen().Timings.Update;
        var dice = Resources.Sandbox["Dice.fbx"].CreateFbxModel();
        var texture = await Resources.Sandbox["Dice.png"].LoadTextureAsync(timing);
        dice.Shader = new PhongShader
        {
            Texture = texture,
        };
        dice.Position = new Vector3(3, 1, -2);
        return await dice.Activate(layer);
    }

    private static UniTask<Model3D> CreateDiceWireframe(ForwardRenderLayer layer)
    {
        var dice = Resources.Sandbox["Dice.fbx"].CreateFbxModel();
        dice.Shader = new WireframeShader();
        dice.Position = new Vector3(5, 1, 0);
        return dice.Activate(layer);
    }

    private static async UniTask<Model3D> CreateDice2(DeferredRenderLayer layer)
    {
        var timing = layer.GetValidScreen().Timings.Update;
        var dice = Resources.Sandbox["Dice.fbx"].CreateFbxModel();
        dice.Position = new Vector3(3, 1, 2);
        dice.Shader = new PbrDeferredShader()
        {
            Texture = await Resources.Sandbox["Dice.png"].LoadTextureAsync(timing),
            Metallic = 0f,
            Roughness = 0.02f,
        };
        return await dice.Activate(layer);
    }

    private static async UniTask<Model3D> CreateModel2(ForwardRenderLayer layer)
    {
        var model = Resources.Sandbox["Alicia/Alicia_solid.pmx"].CreatePmxModel();
        model.Scale = new Vector3(0.3f);
        await model.Activate(layer);
        model.Update.Subscribe(m =>
        {
            var keyborad = m.GetValidScreen().Keyboard;
            if(keyborad.IsPress(Elffy.InputSystem.Keys.Down)) {
                model.Position.Y -= 0.1f;
            }
            if(keyborad.IsPress(Elffy.InputSystem.Keys.Up)) {
                model.Position.Y += 0.1f;
            }
        });
        return model;
    }

    private static UniTask<Model3D> CreateModel3(ObjectLayer layer)
    {
        return Resources.Sandbox["AntiqueCamera.glb"].CreateGlbModel().Activate(layer);
    }

    private static UniTask<SkySphere> CreateSky(ForwardRenderLayer layer)
    {
        var sky = new SkySphere();
        sky.Shader = SkyShader.Instance;
        sky.Scale = new Vector3(500f);
        return sky.Activate(layer);
    }

    private static async UniTask<Plain> CreateFloor(ForwardRenderLayer layer)
    {
        var timing = layer.GetValidScreen().Timings.Update;
        var plain = new Plain
        {
            Shader = new PhongShader
            {
                Texture = await Resources.Sandbox["floor.png"].LoadTextureAsync(TextureConfig.DefaultNearestNeighbor, timing),
            },
            Scale = new Vector3(40f),
            Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian()),
        };
        return await plain.Activate(layer);
    }

    private static async UniTask<Plain> CreateFloor(DeferredRenderLayer layer)
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
                Texture = await Resources.Sandbox["floor.png"].LoadTextureAsync(TextureConfig.DefaultNearestNeighbor, timing),
            },
        };
        return await dice.Activate(layer);
    }

    private static async UniTask<Plain> CreateFloor2(ForwardRenderLayer layer)
    {
        var bufSize = new Vector2i(256, 256);
        var buffer = ShaderStorageBuffer.CreateUninitialized<Color4>(bufSize.X * bufSize.Y);
        var dispatcher = new TestComputeShader(() => buffer.Ssbo).CreateDispatcher();
        var plain = new Plain();
        plain.Position.Z = -10;
        plain.Dead.Subscribe(_ =>
        {
            buffer.Dispose();
            dispatcher.Dispose();
        });
        plain.Update.Subscribe(_ => dispatcher.Dispatch(bufSize.X, bufSize.Y, 1));
        plain.Scale = new Vector3(10f);
        plain.Shader = new TestShader(() => (buffer.Ssbo, bufSize.X, bufSize.Y));
        plain.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian());

        return await plain.Activate(layer);
    }

    private static async UniTask<Cube> CreateBox(DeferredRenderLayer layer)
    {
        var timing = layer.GetValidScreen().Timings.Update;
        var shader = new PbrDeferredShader
        {
            Metallic = 0f,
            Roughness = 0.15f,
        };
        var cube = new Cube
        {
            Position = new(-3, 0.5f, 0),
            Shader = shader,
        };
        cube.Activating.Subscribe(async (_, ct) =>
        {
            shader.Texture = await Resources.Sandbox["box.png"].LoadTextureAsync(timing, ct);
        });
        await cube.Activate(layer);
        cube.Update.Subscribe(cube =>
        {
            SafeCast.As<Cube>(cube).Rotate(Vector3.UnitY, 1f.ToRadian());
        });
        return cube;
    }
}
