#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Graphics.OpenGL;
using Elffy.Shading;
using Elffy.Shading.Deferred;
using Elffy.Graphics;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    public sealed class DeferredRenderingLayer : WorldLayer, IGBufferSource
    {
        private const int MaxLightCount = 1024 * 1024;
        private const int DRLayerDefaultSort = -100;

        private readonly GBuffer _gBuffer;
        private readonly PbrDeferredRenderingPostProcess _postProcess;
        private PostProcessProgram? _ppProgram;

        private bool _isSizeChangeObserved;
        private bool _isSizeChangeRequested;
        private long _sizeChangeRequestedFrameNum;

        IGBuffer IGBufferSource.GBuffer => _gBuffer;

        public DeferredRenderingLayer(int sortNumber = DRLayerDefaultSort) : base(sortNumber)
        {
            _gBuffer = new GBuffer();
            _postProcess = new PbrDeferredRenderingPostProcess(this, static screen => ref screen.Camera.View);
            Activating.Subscribe((l, ct) => SafeCast.As<DeferredRenderingLayer>(l).OnActivating());
        }

        private UniTask OnActivating()
        {
            var screen = Screen;
            Debug.Assert(screen is not null);
            var gBuffer = _gBuffer;
            gBuffer.Initialize(screen);
            _ppProgram = _postProcess.Compile(screen);
            return UniTask.CompletedTask;
        }

        protected override void OnLayerTerminated()
        {
            base.OnLayerTerminated();
            _gBuffer.Dispose();
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

            var gBuffer = _gBuffer;
            var screenSize = screen.FrameBufferSize;
            var gBufSize = gBuffer.Size;

            Debug.Assert(_postProcess is not null);
            Debug.Assert(_ppProgram is not null);
            FBO.Bind(targetFbo, FBO.Target.FrameBuffer);
            if(IsVisible) {
                _ppProgram.Render(screenSize, (Vector2)screenSize / (Vector2)gBufSize);
            }

            FBO.Bind(gBuffer.FBO, FBO.Target.Read);
            FBO.Bind(targetFbo, FBO.Target.Draw);
            var gBufAspect = (float)gBufSize.X / gBufSize.Y;
            var srcRect = new RectI(Vector2i.Zero, gBufSize);
            var destRect = new RectI(0, 0, (int)(gBufSize.Y * gBufAspect), gBufSize.Y);
            Graphic.BlitDepthBuffer(srcRect, destRect);
            FBO.Bind(targetFbo, FBO.Target.FrameBuffer);
            currentFbo = targetFbo;
        }

        protected override void OnSizeChanged(IHostScreen screen)
        {
            base.OnSizeChanged(screen);
            _isSizeChangeRequested = true;
            _sizeChangeRequestedFrameNum = screen.FrameNum;
            if(_isSizeChangeObserved == false) {
                _isSizeChangeObserved = true;
                StartObserveSizeChanged(screen);
            }
        }

        private void StartObserveSizeChanged(IHostScreen screen)
        {
            screen.StartCoroutine(this, static async (co, self) =>
            {
                while(co.CanRun) {
                    if(self._isSizeChangeRequested && co.Screen.FrameNum - self._sizeChangeRequestedFrameNum > 1) {
                        // TODO: when height is 0.
                        self._gBuffer.Resize();
                        Debug.WriteLine("Resize !!!!!!!!!!!!");
                        self._isSizeChangeRequested = false;
                    }
                    await co.TimingPoints.FrameInitializing.Next();
                }
            }, FrameTiming.FrameInitializing).Forget();
        }

        [DoesNotReturn]
        private static void ThrowTooManyLightCount() => throw new ArgumentOutOfRangeException($"Light count is too many. (Max Count: {MaxLightCount})");

        [DoesNotReturn]
        private static void ThrowLightCountIsZeroOrNegative() => throw new ArgumentOutOfRangeException("Light count must be more than one.");
    }

    internal interface IGBufferSource
    {
        IGBuffer GBuffer { get; }

        bool TryGetHostScreen([MaybeNullWhen(false)] out IHostScreen screen);
    }
}
