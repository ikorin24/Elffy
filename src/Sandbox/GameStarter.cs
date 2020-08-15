#nullable enable
using Elffy;
using Elffy.Components;
using Elffy.Animations;
using Elffy.Games;
using Elffy.Imaging;
using Elffy.Mathematics;
using Elffy.Shading;
using Elffy.Shapes;
using Elffy.Threading;
using Elffy.UI;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Effective;

namespace Sandbox
{
    public static class GameStarter
    {
        public static void Start()
        {
            // 光源
            var light = new DirectLight();
            light.Updated += sender =>
            {
                Debug.Assert(sender is DirectLight);
                var light = Unsafe.As<DirectLight>(sender);
                var frameNum = Game.Screen.FrameNum;
                var rotation = new Quaternion(Vector3.UnitX, (frameNum / 60f * 60).ToRadian());
                light.Direction = rotation * new Vector3(0, -1, 0);
            };
            light.Activate();


            // カメラ位置初期化
            var cameraTarget = new Vector3(0, 3, 0);
            Game.MainCamera.LookAt(cameraTarget, new Vector3(0, 4.5f, 20) * 1);


            // マウスでカメラ移動するためのオブジェクト
            var cameraMouse = new CameraMouse(Game.MainCamera, Game.Mouse, cameraTarget);
            cameraMouse.Activate();


            // 床
            var plain = new Plain()
            {
                Scale = new Vector3(20),
                Rotation = new Quaternion(Vector3.UnitX, -90f.ToRadian()),
            };
            plain.AddComponent(new Material(new Color4(0.85f), new Color4(0.15f), new Color4(0.2f), 400f));
            plain.AddComponent(Resources.GetStream("cube.png").ToTexture(BitmapType.Png));
            plain.Activate();


            // キャラ1
            PmxModel.LoadResourceAsync("Alicia/Alicia_solid.pmx").ContinueWithDispatch(model =>
            {
                //model.Scale = new Vector3(0.3f);
                model.AddComponent(new Material(new Color4(0.88f), new Color4(0.18f), new Color4(0.1f), 5f));
                model.Shader = RigShaderSource.Instance;
                model.Activate();
            });


            // サイコロ
            //Model3D.LoadResourceAsync("Dice.fbx").ContinueWithDispatch(model =>
            //{
            //    model.Position = new Vector3(4, 4, -2);
            //    model.Activate();
            //    Animation.Define().Endless(f =>
            //    {
            //        var theta = MathF.PI * 2 * (float)f.Time.TotalSeconds;
            //        var scale = 0.5f + 0.1f * MathF.Sin(theta);
            //        model.Scale = new Vector3(scale);
            //    }).Play();
            //});


            // カエル
            //Model3D.LoadResourceAsync("green_frog.fbx").ContinueWithDispatch(model =>
            //{
            //    model.Scale = new Vector3(0.01f);
            //    model.Position = new Vector3(5, 0, 0);
            //    model.AddComponent(new Material(new Color4(0f, 0.7f, 0.25f), new Color4(0f, 0.6f, 0.1f), Color4.White, 4));
            //    model.Activate();
            //});


            // 箱1
            new Cube()
            {
                Position = new Vector3(-3, 0.5f, 0),
                Shader = NormalShaderSource.Instance,
            }.Activate();


            // 箱2
            var cube = new Cube()
            {
                Position = new Vector3(-3, 0.5f, -3),
            };
            cube.AddComponent(Resources.GetStream("cube.png").ToTexture(BitmapType.Png));
            cube.Updated += sender =>
            {
                Debug.Assert(sender is Cube);
                var cube = Unsafe.As<Cube>(sender);
                var p = Game.Screen.FrameNum / 60f * 30f;
                cube.Rotation = new Quaternion(Vector3.UnitY, p.ToRadian());
            };
            cube.Activate();

            new Sky()
            {
                Scale = new Vector3(500),
                Shader = SkyShaderSource.Instance,
            }.Activate();



            //var test = new TestPlain()
            //{
            //    Position = new Vector3(0, 1, 0),
            //    Shader = TestShaderSource.Instance,
            //};
            //var data = new FloatDataTexture();
            //var array = new Vector4[4];
            //array[0] = new Vector4(2f, 2f, 2f, 1f);
            //array[1] = new Vector4(0f, -5f, -5f, 0f);
            //data.Load(array);
            //test.AddComponent(data);
            //test.Activate();

            InitializeUI();
        }

        private static void InitializeUI()
        {
            //var button = new Button(90, 30);
            //button.KeyDown += sender =>
            //{
            //    Debug.WriteLine("Down");
            //};
            //button.KeyPress += sender => Debug.WriteLine("Press");
            //button.KeyUp += sender => Debug.WriteLine("Up");
            //Game.UI.Add(button);
        }
    }
}
