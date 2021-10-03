#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.Shading.Defered
{
    public sealed class DeferedRenderer
    {
        private const int MaxLightCount = 1024 * 1024;
        private static readonly Vector4 DefaultLightPosition = new Vector4(0, 500, 0, 1);
        private static readonly Color4 DefaultLightColor = Color4.White;

        private readonly LightBuffer _lightBuffer;

        private bool IsDisposed => _lightBuffer.IsDisposed;

        public int LightCount => _lightBuffer?.LightCount ?? 0;

        private DeferedRenderer(LightBuffer lightBuffer)
        {
            _lightBuffer = lightBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateLightPositions(ReadOnlySpan<Vector4> positions)
        {
            UpdateLightPositions(positions, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateLightPositions(ReadOnlySpan<Vector4> positions, int offset)
        {
            if(IsDisposed) { return; }
            _lightBuffer.UpdatePositions(positions, offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateLightColors(ReadOnlySpan<Color4> colors)
        {
            UpdateLightColors(colors, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateLightColors(ReadOnlySpan<Color4> colors, int offset)
        {
            if(IsDisposed) { return; }
            _lightBuffer.UpdateColors(colors, offset);
        }

        public static DeferedRenderer Attach(IHostScreen screen, int lightCount, LightUpdateAction initializeLight)
        {
            var lightBuffer = InitLights(lightCount, initializeLight);
            var renderer = new DeferedRenderer(lightBuffer);
            screen.StartOrReserveCoroutine(renderer, DeferedRenderingPipeline, FrameTiming.EarlyUpdate);
            return renderer;
        }

        private static unsafe LightBuffer InitLights(int lightCount, LightUpdateAction action)
        {
            const int Threshold = 16;

            if((uint)lightCount >= (uint)MaxLightCount) {
                ThrowTooManyLightCount();
            }
            if(action is null) {
                ThrowNullArg(nameof(action));
            }

            if(lightCount <= Threshold) {
                Vector4* positionsPtr = stackalloc Vector4[Threshold];
                Color4* colorsPtr = stackalloc Color4[Threshold];
                var positions = new Span<Vector4>(positionsPtr, lightCount);
                var colors = new Span<Color4>(colorsPtr, lightCount);
                positions.Fill(DefaultLightPosition);
                colors.Fill(DefaultLightColor);
                action(new LightUpdateContext(positions, colors));

                var lightBuffer = new LightBuffer();
                lightBuffer.Initialize(positions, colors);
                return lightBuffer;
            }
            else {
                using var positionsBuf = new ValueTypeRentMemory<Vector4>(lightCount);
                using var colorsBuf = new ValueTypeRentMemory<Color4>(lightCount);
                var positions = positionsBuf.AsSpan();
                var colors = colorsBuf.AsSpan();
                positions.Fill(DefaultLightPosition);
                colors.Fill(DefaultLightColor);
                action(new LightUpdateContext(positions, colors));

                var lightBuffer = new LightBuffer();
                lightBuffer.Initialize(positions, colors);
                return lightBuffer;
            }
        }

        private static async UniTask DeferedRenderingPipeline(CoroutineState coroutine, DeferedRenderer renderer)
        {
            var screen = coroutine.Screen;
            var camera = screen.Camera;
            Debug.Assert(renderer._lightBuffer is not null);
            using var lightBuffer = renderer._lightBuffer;
            using var gBuffer = new GBuffer();
            gBuffer.Initialize(screen);

            var postProcess = new PBRDeferedRenderingPostProcess(gBuffer, lightBuffer);
            var gBufferFBO = gBuffer.FBO;
            using var program = postProcess.Compile(screen);

            while(coroutine.CanRun) {
                await coroutine.BeforeRendering.Switch();
                var resultFBO = FBO.CurrentDrawBinded;
                FBO.Bind(gBufferFBO, FBO.Target.FrameBuffer);
                ElffyGL.Clear(ClearMask.ColorBufferBit | ClearMask.DepthBufferBit);

                await coroutine.AfterRendering.Switch();
                FBO.Bind(resultFBO, FBO.Target.FrameBuffer);
                postProcess.SetMatrices(camera.View);
                program.Render(screen.FrameBufferSize);
            }
        }

        [DoesNotReturn]
        private static void ThrowTooManyLightCount() => throw new ArgumentOutOfRangeException($"Light count is too many. (Max Count: {MaxLightCount})");

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);
    }
}
