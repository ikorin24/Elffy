#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.OpenGL;
using OpenTK.Graphics.OpenGL4;
using System;

namespace Elffy.Shading
{
    public sealed class DeferedRenderer : FrameObject
    {
        protected override void OnActivated()
        {
            base.OnActivated();
            if(TryGetHostScreen(out var screen) == false) { return; }
            Loop(screen).Forget();
        }

        private async UniTaskVoid Loop(IHostScreen screen)
        {
            var camera = screen.Camera;
            var endPoint = screen.AsyncBack;

            using var lightBuffer = InitLight();
            using var gBuffer = InitGBuffer(screen);
            //var postProcess = new DeferedRenderingPostProcess(gBuffer, lightBuffer);
            var postProcess = new PBRDeferedRenderingPostProcess(gBuffer, lightBuffer);
            var fbo = gBuffer.FBO;
            using var program = postProcess.Compile(screen);

            while(screen.IsRunning && LifeState.IsBefore(LifeState.Dead)) {
                await endPoint.ToTiming(FrameLoopTiming.BeforeRendering);
                var currentFbo = FBO.CurrentDrawBinded;
                FBO.Bind(fbo, FBO.Target.FrameBuffer);
                ElffyGL.Clear(ClearMask.ColorBufferBit | ClearMask.DepthBufferBit);
                postProcess.SetMatrices(camera.View);

                await endPoint.ToTiming(FrameLoopTiming.AfterRendering);
                FBO.Bind(currentFbo, FBO.Target.FrameBuffer);
                program.Render(screen.FrameBufferSize);
            }
        }

        private static LightBuffer InitLight()
        {
            const int LightCount = 4;
            Span<Vector4> pos = stackalloc Vector4[LightCount];
            Span<Color4> color = stackalloc Color4[LightCount];
            pos[0] = new Vector4(0, 1000, 0, 1);
            pos[1] = new Vector4(0, 0, 0, 0);
            pos[2] = new Vector4(0, 0, 0, 0);
            pos[3] = new Vector4(0, 0, 0, 0);
            color[0] = new Color4(1, 1, 1, 1);
            color[1] = new Color4(1, 1, 1, 1);
            color[2] = new Color4(1, 1, 1, 1);
            color[3] = new Color4(1, 1, 1, 1);

            var lightBuffer = new LightBuffer();
            try {
                lightBuffer.Initialize(pos, color);
                return lightBuffer;
            }
            catch {
                lightBuffer.Dispose();
                throw;
            }
        }

        private static GBuffer InitGBuffer(IHostScreen screen)
        {
            var gBuffer = new GBuffer();
            gBuffer.Initialize(screen);
            return gBuffer;
        }
    }
}
