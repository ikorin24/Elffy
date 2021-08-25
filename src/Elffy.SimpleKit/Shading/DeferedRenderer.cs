#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.OpenGL;
using System;

namespace Elffy.Shading
{
    public sealed class DeferedRenderer
    {
        private DeferedRenderer()
        {
        }

        public static DeferedRenderer Attach(IHostScreen screen)
        {
            var renderer = new DeferedRenderer();
            Coroutine.Create(screen, renderer, DeferedRenderingPipeline);
            return renderer;
        }

        private static async UniTask DeferedRenderingPipeline(CoroutineState coroutine, DeferedRenderer renderer)
        {
            var screen = coroutine.Screen;
            var camera = screen.Camera;
            using var lightBuffer = InitLight();
            using var gBuffer = InitGBuffer(screen);
            var postProcess = new PBRDeferedRenderingPostProcess(gBuffer, lightBuffer);
            var gBufferFBO = gBuffer.FBO;
            using var program = postProcess.Compile(screen);

            while(coroutine.CanRun) {
                await coroutine.ToBeforeRendering();
                var resultFBO = FBO.CurrentDrawBinded;
                FBO.Bind(gBufferFBO, FBO.Target.FrameBuffer);
                ElffyGL.Clear(ClearMask.ColorBufferBit | ClearMask.DepthBufferBit);

                await coroutine.ToAfterRendering();
                FBO.Bind(resultFBO, FBO.Target.FrameBuffer);
                postProcess.SetMatrices(camera.View);
                program.Render(screen.FrameBufferSize);
            }
        }

        private static LightBuffer InitLight()
        {
            const int LightCount = 4;
            Span<Vector4> pos = stackalloc Vector4[LightCount];
            Span<Color4> color = stackalloc Color4[LightCount];
            pos[0] = new Vector4(500, 100, 400, 1);
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
