#nullable enable
using System;
using Cysharp.Threading.Tasks;
using Elffy;
using Elffy.Mathematics;
using Elffy.Shapes;
using Elffy.Shading.Deferred;
using Elffy.Shading.Forward;
using Elffy.Components;
using Elffy.Effective;
using Elffy.UI;
using System.Diagnostics;
using System.Linq;

namespace Sandbox
{
    public static class Startup
    {
        [GameEntryPoint]
        public static async UniTask Start2()
        {
            var screen = Game.Screen;
            var (drLayer, wLayer, uiLayer) = await LayerPipelines.UseDeferredForward(screen);
            var uiRoot = uiLayer.UIRoot;
            /*
             context =>
                {
                    var interval = 100;
                    for(int i = 0; i < context.LightCount; i++) {
                        var x = i * interval - context.LightCount * interval / 2;
                        var pos = new Vector3(x, 10, 0);
                        context.SetPointLight(i, pos, Color4.White);
                    }
                }
             
             */
            //CreateTestUI(uiLayer);
            var timings = screen.TimingPoints;
            uiRoot.Background = Color4.Black;
            try {
                await UniTask.WhenAll(
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


        private static void CreateTestUI(UILayer uiLayer)
        {
            var uiRoot = uiLayer.UIRoot;

            const int ColumnCount = 6;
            var gridLength = LayoutLength.Proportion(1f / ColumnCount);
            var grid = new Grid();
            grid.ColumnDefinition(Enumerable.Repeat(gridLength, ColumnCount).ToArray());
            uiRoot.Children.Add(grid);

            for(int i = 0; i < ColumnCount; i++) {
                var button = new Button();
                button.SetGridColumn(grid, i);
                button.Width = 100;
                button.Height = 100;
                button.Background = Color4.Red;
                button.KeyUp += _ => Debug.WriteLine($"Clicked");
                grid.Children.Add(button);
            }
        }

        private static UniTask<Model3D> CreateDice(WorldLayer layer)
        {
            var dice = Resources.Sandbox["Dice.fbx"].CreateFbxModel();
            dice.Shader = new PhongShader(Color3.Aquamarine);
            dice.Position = new Vector3(3, 1, -2);
            return dice.Activate(layer);
        }

        private static UniTask<Model3D> CreateDice2(DeferredRenderingLayer layer)
        {
            var dice = Resources.Sandbox["Dice.fbx"].CreateFbxModel();
            dice.Position = new Vector3(3, 1, 2);
            dice.Shader = new PbrDeferredShader()
            {
                Albedo = new Color3(1, 0.8f, 0.2f),
                Metallic = 0.99f,
                Roughness = 0.1f,
            };
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
