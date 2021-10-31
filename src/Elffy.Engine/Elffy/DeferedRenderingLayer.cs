#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Graphics.OpenGL;
using Elffy.Shading;
using Elffy.Shading.Defered;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Elffy
{
    public sealed class DeferedRenderingLayer : WorldLayer
    {
        private const int MaxLightCount = 1024 * 1024;
        private const int DRLayerDefaultSort = -100;
        private const int PPLayerDefaultSort = 1000;

        private readonly DeferedRenderingPostProcessLayer _ppLayer;
        private readonly GBuffer _gBuffer;
        private readonly LightBuffer _lightBuffer;
        private readonly int _lightCount;

        public DeferedRenderingLayer(string name, int lightCount, int sortNumber = DRLayerDefaultSort, int postProcessSortNumber = PPLayerDefaultSort) : base(name, sortNumber)
        {
            if(lightCount <= 0) { ThrowLightCountIsZeroOrNegative(); }
            if(lightCount > MaxLightCount) { ThrowTooManyLightCount(); }
            _lightCount = lightCount;
            _ppLayer = new DeferedRenderingPostProcessLayer(postProcessSortNumber);
            _gBuffer = new GBuffer();
            _lightBuffer = new LightBuffer();
            Activating.Subscribe((l, ct) => SafeCast.As<DeferedRenderingLayer>(l).OnActivating(ct));
        }

        public static async UniTask<DeferedRenderingLayer> NewActivate(
            IHostScreen screen,
            string name,
            int lightCount,
            int sortNumber = DRLayerDefaultSort,
            int postProcessSortNumber = PPLayerDefaultSort,
            CancellationToken cancellationToken = default)
        {
            var drlayer = new DeferedRenderingLayer(name, lightCount, sortNumber, postProcessSortNumber);
            return await drlayer.Activate(screen, cancellationToken);
        }

        private async UniTask OnActivating(CancellationToken ct)
        {
            var screen = Screen;
            var ppLayer = _ppLayer;
            Debug.Assert(screen is not null);
            await ppLayer.Activate(screen, ct);

            var lightBuffer = _lightBuffer;
            var gBuffer = _gBuffer;
            lightBuffer.Initialize(_lightCount);
            gBuffer.Initialize(screen);
            var postProcess = new PbrDeferedRenderingPostProcess(gBuffer, lightBuffer, static screen => ref screen.Camera.View);
            ppLayer.InitializePostPorcess(screen, postProcess);
        }

        protected override void OnLayerTerminated()
        {
            base.OnLayerTerminated();
            _gBuffer.Dispose();
            _lightBuffer.Dispose();
        }

        protected override void OnRendering(IHostScreen screen)
        {
            FBO.Bind(_gBuffer.FBO, FBO.Target.FrameBuffer);
            ElffyGL.Clear(ClearMask.ColorBufferBit | ClearMask.DepthBufferBit);
        }

        [DoesNotReturn]
        private static void ThrowTooManyLightCount() => throw new ArgumentOutOfRangeException($"Light count is too many. (Max Count: {MaxLightCount})");

        [DoesNotReturn]
        private static void ThrowLightCountIsZeroOrNegative() => throw new ArgumentOutOfRangeException("Light count must be more than one.");
    }

    internal sealed class DeferedRenderingPostProcessLayer : PostProcessLayer
    {
        private PbrDeferedRenderingPostProcess? _postProcess;
        private PostProcessProgram? _ppProgram;

        internal DeferedRenderingPostProcessLayer(int sortNumber) : base("Defered Rendering Post Process", sortNumber)
        {
        }

        internal void InitializePostPorcess(IHostScreen screen, PbrDeferedRenderingPostProcess postProcess)
        {
            _postProcess = postProcess;
            _ppProgram = postProcess.Compile(screen);
        }

        protected override void OnAlive(IHostScreen screen)
        {
        }

        protected override void OnLayerTerminated()
        {
            _ppProgram?.Dispose();
            _ppProgram = null;
            _postProcess = null;
        }

        protected override void OnSizeChanged(IHostScreen screen)
        {
        }

        protected override void RenderPostProcess(IHostScreen screen)
        {
            Debug.Assert(_postProcess is not null);
            Debug.Assert(_ppProgram is not null);
            FBO.Bind(FBO.Empty, FBO.Target.FrameBuffer);
            _ppProgram.Render(screen.FrameBufferSize);
        }
    }
}
