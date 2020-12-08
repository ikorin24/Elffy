#nullable enable
using System;
using Elffy;
using Elffy.OpenGL;
using Elffy.Shapes;
using Elffy.Shading;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Sandbox
{
    public static class Startup
    {
        public static async void Start()
        {
            Definition.Initialize();

            Hoge().Forget();

            await Definition.GenCameraMouse();
            new Cube()
            {
                Shader = DeferedRenderingShaderSource.Instance,
            }.Activate();

            for(int x = -3; x < 4; x++) {
                for(int y = -3; y < 4; y++) {
                    var d = Resources.Loader.CreateFbxModel("Dice.fbx");
                    d.Shader = DeferedRenderingShaderSource.Instance;
                    d.Position = new(x, y, 0);
                    d.Scale = new(0.4f);
                    d.Activate();
                }
            }

            //await Definition.GenUI();

            //var dice = await Definition.GenDice();
            //var behavior = await Definition.GenDiceBehavior();
            //behavior(dice).Forget();

            //Definition.GenKeyBoardInputDump().Forget();

            //await UniTask.WhenAll(
            //    Definition.GenCameraMouse(),
            //    Definition.GenPlain(),
            //    Definition.GenAlicia(),
            //    Definition.GenFrog(),
            //    Definition.GenBox2(),
            //    Definition.GenSky());

        }

        static async UniTaskVoid Hoge()
        {
            var tmp = new Cube() { IsVisible = false, };
            var gBuffer = new GBuffer(Game.Screen);
            var program = gBuffer.PostProcess.Compile();
            tmp.Terminated += _ =>
            {
                gBuffer.Dispose();
                program.Dispose();
            };
            tmp.Activate();
            while(true) {
                await Timing.ToBeforeRendering();
                gBuffer.BindFrameBuffer();
                ElffyGL.Clear(ElffyGL.ClearBufferMask.ColorBufferBit | ElffyGL.ClearBufferMask.DepthBufferBit);

                await Timing.ToAfterRendering();
                FBO.Unbind(FBO.Target.FrameBuffer);
                program.Render(Game.Screen.ClientSize);
            }



            ////var gBuffer = new GBuffer(Game.Screen);
            ////Game.Screen.PostProcess = gBuffer.PostProcess;
            //try {
            //    while(true) {
            //        await GameAsync.ToBeforeRendering(Game.Screen.RunningToken);
            //        //gBuffer.BindFrameBuffer();
            //        await GameAsync.ToAfterRendering(Game.Screen.RunningToken);
            //    }
            //}
            //finally {
            //    //gBuffer.Dispose();
            //    System.Diagnostics.Debug.WriteLine("close");
            //}
        }
    }
}
