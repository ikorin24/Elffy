#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Graphics.OpenGL;
using Elffy.Shading;
using Elffy.Shading.Defered;
using Elffy.Graphics;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    public sealed class DeferedRenderingLayer : WorldLayer
    {
        private const int MaxLightCount = 1024 * 1024;
        private const int DRLayerDefaultSort = -100;

        private readonly GBuffer _gBuffer;
        private readonly LightBuffer _lightBuffer;
        private readonly PbrDeferedRenderingPostProcess _postProcess;
        private readonly int _lightCount;
        private PostProcessProgram? _ppProgram;

        public DeferedRenderingLayer(int lightCount, int sortNumber = DRLayerDefaultSort) : base(sortNumber)
        {
            if(lightCount <= 0) { ThrowLightCountIsZeroOrNegative(); }
            if(lightCount > MaxLightCount) { ThrowTooManyLightCount(); }
            _lightCount = lightCount;
            _gBuffer = new GBuffer();
            _lightBuffer = new LightBuffer();
            _postProcess = new PbrDeferedRenderingPostProcess(_gBuffer, _lightBuffer, static screen => ref screen.Camera.View);
            Activating.Subscribe((l, ct) => SafeCast.As<DeferedRenderingLayer>(l).OnActivating());
        }

        private UniTask OnActivating()
        {
            var screen = Screen;
            Debug.Assert(screen is not null);

            var lightBuffer = _lightBuffer;
            var gBuffer = _gBuffer;
            lightBuffer.Initialize(_lightCount);
            gBuffer.Initialize(screen);
            _ppProgram = _postProcess.Compile(screen);
            return UniTask.CompletedTask;
        }

        protected override void OnLayerTerminated()
        {
            base.OnLayerTerminated();
            _gBuffer.Dispose();
            _lightBuffer.Dispose();

            _ppProgram?.Dispose();
            _ppProgram = null;
        }

        protected override void OnRendering(IHostScreen screen, ref FBO currentFbo)
        {
            currentFbo = _gBuffer.FBO;
            FBO.Bind(currentFbo, FBO.Target.FrameBuffer);
            ElffyGL.Clear(ClearMask.ColorBufferBit | ClearMask.DepthBufferBit);
        }

        protected override void OnRendered(IHostScreen screen, ref FBO currentFbo)
        {
            var targetFbo = FBO.Empty;

            Debug.Assert(_postProcess is not null);
            Debug.Assert(_ppProgram is not null);
            FBO.Bind(targetFbo, FBO.Target.FrameBuffer);
            if(IsVisible) {
                _ppProgram.Render(screen.FrameBufferSize);
            }
            FBO.Bind(_gBuffer.FBO, FBO.Target.Read);
            FBO.Bind(targetFbo, FBO.Target.Draw);
            Graphic.BlitDepthBuffer(screen.FrameBufferSize);
            FBO.Bind(targetFbo, FBO.Target.FrameBuffer);
            currentFbo = targetFbo;
        }

        [DoesNotReturn]
        private static void ThrowTooManyLightCount() => throw new ArgumentOutOfRangeException($"Light count is too many. (Max Count: {MaxLightCount})");

        [DoesNotReturn]
        private static void ThrowLightCountIsZeroOrNegative() => throw new ArgumentOutOfRangeException("Light count must be more than one.");
    }
}
