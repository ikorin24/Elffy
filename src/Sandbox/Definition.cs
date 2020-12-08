#nullable enable
using Elffy;
using Elffy.Components;
using Elffy.Imaging;
using Elffy.Mathematics;
using Elffy.Shading;
using Elffy.Shapes;
using Elffy.UI;
using Elffy.Diagnostics;
using Elffy.Threading.Tasks;
using Elffy.InputSystem;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using SkiaSharp;
using System.Threading;
using Elffy.Core;

namespace Sandbox
{
    // 将来的に Source Generator で似たような物を自動生成したい
    // I would like to create this class automatically by Source Generator in the future...

    public static class Definition
    {
        private static int _initialized;

        private const uint ID_Light = 0;
        private const uint ID_CameraMouse = 1;
        private const uint ID_Plain = 2;
        private const uint ID_Alicia = 3;
        private const uint ID_Dice = 4;
        private const uint ID_DiceBehavior = 5;
        private const uint ID_Frog = 6;
        //private const uint ID_Box = 7;
        private const uint ID_Box2 = 8;
        private const uint ID_Sky = 9;
        private const uint ID_UI = 10;
        private const uint ID_KeyBoardInputDump = 11;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Initialize()
        {
            var isInit = Interlocked.CompareExchange(ref _initialized, 1, 0);
            if(isInit == 1) { return; }
            Init();

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Init()
            {
                ObjectFactory.Register(ID_Light, DefineLight);
                ObjectFactory.Register(ID_CameraMouse, DefineCameraMouse);
                ObjectFactory.Register(ID_Plain, DefinePlain);
                ObjectFactory.Register(ID_Alicia, DefineAlicia);
                ObjectFactory.Register(ID_Dice, DefineDice);
                ObjectFactory.Register(ID_DiceBehavior, DefineDiceBehavior);
                ObjectFactory.Register(ID_Frog, DefineFrog);
                //ObjectFactory.Register(ID_Box, DefineBox);
                ObjectFactory.Register(ID_Box2, DefineBox2);
                ObjectFactory.Register(ID_Sky, DefineSky);
                ObjectFactory.Register(ID_UI, DefineUI);
                ObjectFactory.Register(ID_KeyBoardInputDump, DefineKeyBoardInputDump);
            }
        }

        #region Light
        public static UniTask<Light> GenLight() => ObjectFactory.GenerateAsync<Light>(ID_Light);

        private static UniTask<Light> DefineLight()
        {
            var light = new DirectLight();
            light.Updated += sender =>
            {
                Debug.Assert(sender is DirectLight);
                var light = Unsafe.As<DirectLight>(sender);
                var frameNum = Game.FrameNum;
                var rotation = new Quaternion(Vector3.UnitX, (frameNum / 60f * 60).ToRadian());
                light.Direction = rotation * new Vector3(0, -1, 0);
            };
            light.Activate();
            return new(light);
        }
        #endregion Light

        #region CameraMouse
        public static UniTask<CameraMouse> GenCameraMouse() => ObjectFactory.GenerateAsync<CameraMouse>(ID_CameraMouse);

        private static UniTask<CameraMouse> DefineCameraMouse()
        {
            var cameraTarget = new Vector3(0, 3, 0);
            Game.Camera.LookAt(cameraTarget, new Vector3(0, 4.5f, 20) * 1);
            var cameraMouse = new CameraMouse(Game.Camera, Game.Mouse, cameraTarget);
            cameraMouse.Activate();
            return new(cameraMouse);
        }
        #endregion CameraMouse

        #region Plain
        public static UniTask<Plain> GenPlain() => ObjectFactory.GenerateAsync<Plain>(ID_Plain);

        private static UniTask<Plain> DefinePlain()
        {
            var plain = new Plain()
            {
                Scale = new Vector3(10),
                Rotation = new Quaternion(Vector3.UnitX, -90f.ToRadian()),
                Shader = TextureShaderSource<Vertex>.Instance,
            };
            plain.AddComponent(new Material(new Color4(1f), new Color4(0.25f), new Color4(0.2f), 400f));
            plain.AddComponent(Resources.Loader.LoadTexture("floor.png", BitmapType.Png));
            plain.Activate();
            return new(plain);
        }
        #endregion Plain

        #region Alicia
        public static UniTask<PmxModel> GenAlicia() => ObjectFactory.GenerateAsync<PmxModel>(ID_Alicia);

        private static async UniTask<PmxModel> DefineAlicia()
        {
            var model = await Resources.Loader.LoadPmxModelAsync("Alicia/Alicia_solid.pmx");
            await Timing.ToUpdate();
            model.Scale = new Vector3(0.3f);
            model.AddComponent(new Material(new Color4(0.88f), new Color4(0.18f), new Color4(0.1f), 5f));
            model.Shader = RigShaderSource.Instance;
            model.Activate();
            return model;
        }

        //public static UniTask<Model3D> GenAlicia() => ObjectFactory.GenerateAsync<Model3D>(ID_Alicia);

        //private static UniTask<Model3D> DefineAlicia()
        //{
        //    var model = Elffy.Serialization.PmxModelBuilder.CreateLazyLoadingPmx(Resources.Loader, "Alicia/Alicia_solid.pmx");
        //    model.Scale = new Vector3(0.3f);
        //    model.AddComponent(new Material(new Color4(0.88f), new Color4(0.18f), new Color4(0.1f), 5f));
        //    model.Shader = RigShaderSource.Instance;
        //    model.Activate();
        //    return new UniTask<Model3D>(model);
        //}
        #endregion Alicia

        #region Dice
        public static UniTask<Model3D> GenDice() => ObjectFactory.GenerateAsync<Model3D>(ID_Dice);

        private static UniTask<Model3D> DefineDice()
        {
            var model = Resources.Loader.CreateFbxModel("Dice.fbx");
            model.Shader = PhongShaderSource.Instance;
            model.Position = new Vector3(4, 4, -2);
            model.Activate();
            return new UniTask<Model3D>(model);
        }
        #endregion

        #region DiceBehavior
        public static UniTask<Func<Model3D, UniTaskVoid>> GenDiceBehavior() => ObjectFactory.GenerateAsync<Func<Model3D, UniTaskVoid>>(ID_DiceBehavior);

        private static UniTask<Func<Model3D, UniTaskVoid>> DefineDiceBehavior()
        {
            return new UniTask<Func<Model3D, UniTaskVoid>>(async (Model3D model) =>
            {
                var startTime = Game.Time.TotalSeconds;
                while(!model.IsDead && !model.IsFrozen) {
                    var theta = MathF.PI * 2 * (float)(startTime - Game.Time.TotalSeconds);
                    var scale = 0.5f + 0.1f * MathF.Sin(theta);
                    model.Scale = new Vector3(scale);
                    await Timing.ToUpdate();
                }
            });
        }
        #endregion

        #region Frog
        public static UniTask<Model3D> GenFrog() => ObjectFactory.GenerateAsync<Model3D>(ID_Frog);

        private static UniTask<Model3D> DefineFrog()
        {
            var model = Resources.Loader.CreateFbxModel("green_frog.fbx");
            model.Shader = PhongShaderSource.Instance;
            model.Scale = new Vector3(0.01f);
            model.Position = new Vector3(5, 0, 0);
            model.AddComponent(new Material(new Color4(0f, 0.7f, 0.25f), new Color4(0f, 0.6f, 0.1f), Color4.White, 4));
            model.Activate();
            return new UniTask<Model3D>(model);
        }
        #endregion

        //#region Box
        //public static UniTask<Cube> GenBox() => ObjectFactory.GenerateAsync<Cube>(ID_Box);

        //private static UniTask<Cube> DefineBox()
        //{
        //    var cube = new Cube()
        //    {
        //        Position = new Vector3(-3, 0.5f, 0),
        //        Shader = NormalShaderSource.Instance,
        //    };
        //    cube.Activate();
        //    return new UniTask<Cube>(cube);
        //}
        //#endregion

        #region Box2
        public static UniTask<Cube> GenBox2() => ObjectFactory.GenerateAsync<Cube>(ID_Box2);

        private static UniTask<Cube> DefineBox2()
        {
            var cube = new Cube()
            {
                Position = new Vector3(-3, 0.5f, -3),
                Shader = PhongShaderSource.Instance,
            };
            cube.AddComponent(Resources.Loader.LoadTexture("box.png", BitmapType.Png));
            cube.Updated += sender =>
            {
                Debug.Assert(sender is Cube);
                var cube = Unsafe.As<Cube>(sender);
                var p = Game.FrameNum / 60f * 30f;
                cube.Rotation = new Quaternion(Vector3.UnitY, p.ToRadian());
            };
            cube.Activate();
            return new UniTask<Cube>(cube);
        }
        #endregion

        #region Sky
        public static UniTask<SkySphere> GenSky() => ObjectFactory.GenerateAsync<SkySphere>(ID_Sky);

        private static UniTask<SkySphere> DefineSky()
        {
            var sky = new SkySphere()
            {
                Scale = new Vector3(500),
                Shader = SkyShaderSource.Instance,
            };
            sky.Activate();
            return new UniTask<SkySphere>(sky);
        }
        #endregion

        #region UI
        public static UniTask GenUI() => ObjectFactory.GenerateNoneAsync(ID_UI);

        private static UniTask DefineUI()
        {
            using var typeface = Resources.Loader.LoadTypeface("mplus-1p-regular.otf");
            using var font = new SKFont(typeface, size: 12);

            var button = new Button(90, 26);
            using(var p = button.GetPainter()) {
                p.DrawText("Button", font, new Vector2(24, 17), ColorByte.Black);
            }
            button.Position = new Vector2i(10, 10);
            button.KeyDown += sender => DevEnv.WriteLine("Down");
            button.KeyPress += sender => DevEnv.WriteLine("Press");
            button.KeyUp += sender => DevEnv.WriteLine("Up");
            GameUI.Root.Children.Add(button);
            return UniTask.CompletedTask;
        }
        #endregion

        #region KeyBoardInputDump
        public static UniTask GenKeyBoardInputDump() => ObjectFactory.GenerateNoneAsync(ID_KeyBoardInputDump);

        private static async UniTask DefineKeyBoardInputDump()
        {
            while(true) {
                await Timing.ToUpdate();
                if(Game.Keyboard.IsDown(Keys.S, KeyModifiers.Control)) {
                    Debug.WriteLine($"{Game.FrameNum}: Ctrl+S down");
                }
                if(Game.Keyboard.IsPress(Keys.S, KeyModifiers.Control)) {
                    Debug.WriteLine($"{Game.FrameNum}: Ctrl+S press");
                }
                if(Game.Keyboard.IsUp(Keys.S, KeyModifiers.Control)) {
                    Debug.WriteLine($"{Game.FrameNum}: Ctrl+S up");
                }

                if(Game.Keyboard.IsDown(Keys.A)) {
                    Debug.WriteLine($"{Game.FrameNum}: A down");
                }
                if(Game.Keyboard.IsPress(Keys.A)) {
                    Debug.WriteLine($"{Game.FrameNum}: A press");
                }
                if(Game.Keyboard.IsUp(Keys.A)) {
                    Debug.WriteLine($"{Game.FrameNum}: A up");
                }
            }
        }
        #endregion
    }
}
