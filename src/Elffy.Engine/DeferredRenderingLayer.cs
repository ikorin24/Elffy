#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Graphics.OpenGL;
using Elffy.Shading;
using Elffy.Shading.Deferred;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;

namespace Elffy
{
    public sealed class DeferredRenderingLayer : WorldLayer, IGBufferProvider
    {
        private const int DRLayerDefaultSort = -100;

        private readonly GBuffer _gBuffer;
        private readonly PbrDeferredPostProcess _postProcess;
        private PostProcessProgram? _ppProgram;

        private bool _isSizeChangeObserved;
        private bool _isSizeChangeRequested;
        private long _sizeChangeRequestedFrameNum;
        private bool _isBlendEnabledCache;

        public DeferredRenderingLayer(int sortNumber = DRLayerDefaultSort) : base(sortNumber)
        {
            _gBuffer = new GBuffer();
            _postProcess = new PbrDeferredPostProcess(this);
            Activating.Subscribe((l, ct) => SafeCast.As<DeferredRenderingLayer>(l).OnActivating());
        }

        public GBufferData GetGBufferData()
        {
            return _gBuffer.GetBufferData();
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

        protected override void OnDead()
        {
            base.OnDead();
            _gBuffer.Dispose();
            _ppProgram?.Dispose();
            _ppProgram = null;
        }

        protected override void OnRendering(IHostScreen screen, ref FBO currentFbo)
        {
            currentFbo = _gBuffer.FBO;
            FBO.Bind(currentFbo, FBO.Target.FrameBuffer);
            ElffyGL.Clear(ClearMask.ColorBufferBit | ClearMask.DepthBufferBit);
            bool isBlendEnabled = GL.GetInteger(GetPName.Blend) != 0;
            _isBlendEnabledCache = isBlendEnabled;
            if(isBlendEnabled) {
                GL.Disable(EnableCap.Blend);
            }
            _gBuffer.ClearColorBuffers();
        }

        protected override void OnRendered(IHostScreen screen, ref FBO currentFbo)
        {
            if(_isBlendEnabledCache) {
                GL.Enable(EnableCap.Blend);
            }
            var targetFbo = FBO.Empty;

            var gBuffer = _gBuffer;
            var screenSize = screen.FrameBufferSize;
            var gBufSize = gBuffer.Size;

            Debug.Assert(_postProcess is not null);
            Debug.Assert(_ppProgram is not null);
            FBO.Bind(targetFbo, FBO.Target.FrameBuffer);
            if(IsEnabled) {
                _ppProgram.Render(screenSize, (Vector2)screenSize / (Vector2)gBufSize);
            }
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
                        self._isSizeChangeRequested = false;
                    }
                    await co.TimingPoints.FrameInitializing.Next();
                }
            }, FrameTiming.FrameInitializing).Forget();
        }
    }

    internal interface IGBufferProvider
    {
        bool TryGetScreen([MaybeNullWhen(false)] out IHostScreen screen);

        GBufferData GetGBufferData();

        public IHostScreen GetValidScreen()
        {
            if(TryGetScreen(out var screen) == false) {
                ThrowHelper.ThrowInvalidNullScreen();
            }
            return screen;
        }
    }
}
