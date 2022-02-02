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

namespace Sandbox
{
    public static class Startup
    {
        [GameEntryPoint]
        public static async UniTask Start()
        {
            var screen = Game.Screen;
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
                    CreateTestUI(uiLayer),
                    CreateDice2(drLayer),
                    CreateCameraMouse(wLayer, new Vector3(0, 3, 0)),
                    CreateDice(wLayer),
                    CreateModel2(wLayer),
                    CreateBox(wLayer),
                    CreateFloor(wLayer),
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
            ReadOnlySpan<LightData> lights = stackalloc LightData[]
            {
                new()
                {
                    Color = Color3.White,
                    Position = new Vector3(1, 1, 0),
                    Type = LightType.DirectLight,
                },
                //new()
                //{
                //    Color = Color3.OrangeRed,
                //    Position = new Vector3(1, 0, 0),
                //    Type = LightType.DirectLight,
                //},
            };
            screen.Lights.StaticLights.Initialize(lights);
        }

        private static UniTask CreateTestUI(UILayer uiLayer)
        {
            var uiRoot = uiLayer.UIRoot;
            var tasks = new ParallelOperation();

            const int ColumnCount = 6;
            var gridLength = LayoutLength.Length(200);
            var grid = new Grid();
            grid.ColumnDefinition(stackalloc LayoutLength[2]
            {
                LayoutLength.Length(160),
                LayoutLength.Proportion(1f),
            });
            tasks.Add(uiRoot.Children.Add(grid));

            var leftPanel = new Grid()
            {
                Background = Color3.Purple.ToColor4(0.5f),
                Padding = new LayoutThickness(5, 5, 5, 5),
            };
            leftPanel.SetGridColumn(grid, 0);
            leftPanel.RowDefinition(stackalloc LayoutLength[ColumnCount]
            {
                LayoutLength.Length(60),
                LayoutLength.Length(60),
                LayoutLength.Length(60),
                LayoutLength.Length(60),
                LayoutLength.Length(60),
                LayoutLength.Length(60),
            });
            tasks.Add(grid.Children.Add(leftPanel));

            for(int i = 0; i < ColumnCount; i++) {
                var button = new Button
                {
                    Width = 120,
                    Height = 45,
                    Background = new Color4(0.8f),
                    Shader = new CustomUIShader
                    {
                        CornerRadius = new Vector4(4),
                    },
                };
                button.SetGridRow(leftPanel, i);
                button.KeyUp += _ => Debug.WriteLine($"Clicked");
                tasks.Add(leftPanel.Children.Add(button));
            }
            return tasks.WhenAll();
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
            var plain = new Plain();
            plain.Scale = new Vector3(10f);
            plain.Shader = new PhongShader();
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
            cube.Shader = new PhongShader();
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
