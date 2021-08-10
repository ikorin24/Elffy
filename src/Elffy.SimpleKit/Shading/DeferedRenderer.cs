#nullable enable
using System;
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.OpenGL;

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
            using var lightBuffer = InitLight();
            var camera = screen.Camera;
            var endPoint = screen.AsyncBack;
            using var gBuffer = new GBuffer();
            var postProcess = gBuffer.Initialize(screen, lightBuffer.GetPositions(), lightBuffer.GetColors());
            using var ppp = postProcess.Compile();
            while(screen.IsRunning && LifeState.IsBefore(LifeState.Dead)) {
                await endPoint.ToTiming(FrameLoopTiming.BeforeRendering);
                var currentFbo = FBO.CurrentDrawBinded;
                FBO.Bind(gBuffer.FBO, FBO.Target.FrameBuffer);
                ElffyGL.Clear(ClearMask.ColorBufferBit | ClearMask.DepthBufferBit);
                postProcess.SetMatrices(camera.View, camera.Projection);

                await endPoint.ToTiming(FrameLoopTiming.AfterRendering);
                FBO.Bind(currentFbo, FBO.Target.FrameBuffer);
                ppp.Render(screen.FrameBufferSize);
            }
        }

        private static LightBuffer InitLight()
        {
            const int LightCount = 4;
            var lightBuffer = new LightBuffer(LightCount, false);
            var pos = lightBuffer.GetPositions();
            var color = lightBuffer.GetColors();
            pos[0] = new Vector4(1, 1, 0, 0);
            pos[1] = new Vector4(0, 0, 0, 0);
            pos[2] = new Vector4(0, 0, 0, 0);
            pos[3] = new Vector4(0, 0, 0, 0);
            color[0] = new Color4(1, 1, 1, 1);
            color[1] = new Color4(1, 1, 1, 1);
            color[2] = new Color4(1, 1, 1, 1);
            color[3] = new Color4(1, 1, 1, 1);
            return lightBuffer;
        }

        private readonly struct LightBuffer : IDisposable
        {
            private readonly ValueTypeRentMemory<Vector4> _positions;
            private readonly ValueTypeRentMemory<Color4> _colors;

            public LightBuffer(int count, bool zeroFill)
            {
                ValueTypeRentMemory<Vector4> positions = default;
                ValueTypeRentMemory<Color4> colors = default;
                try {
                    positions = new ValueTypeRentMemory<Vector4>(count, zeroFill);
                    colors = new ValueTypeRentMemory<Color4>(count, zeroFill);
                }
                catch {
                    positions.Dispose();
                    colors.Dispose();
                    throw;
                }
                _positions = positions;
                _colors = colors;
            }

            public Span<Vector4> GetPositions() => _positions.AsSpan();
            public Span<Color4> GetColors() => _colors.AsSpan();

            public void Dispose()
            {
                _positions.Dispose();
                _colors.Dispose();
            }
        }
    }
}
