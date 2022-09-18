#nullable enable
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Elffy.Graphics.OpenGL;
using Elffy.Shading;
using Elffy.Shading.Deferred;
using OpenTK.Graphics.OpenGL4;

namespace Elffy
{
    public sealed class DeferredRenderLayer : ObjectLayer, IGBufferProvider
    {
        private const int DefaultSortNumber = -100;
        private readonly FBO _renderTarget;
        private readonly GBuffer _gBuffer;
        private readonly PbrDeferredPostProcess _postProcess;
        private PostProcessProgram? _ppProgram;
        private bool _isSizeChangeObserved;
        private bool _isSizeChangeRequested;
        private long _sizeChangeRequestedFrameNum;
        private bool _isBlendEnabledCache;

        public DeferredRenderLayer(int sortNumber = DefaultSortNumber) : this(FBO.Empty, sortNumber) { }

        public DeferredRenderLayer(FBO renderTarget, int sortNumber = DefaultSortNumber) : base(sortNumber)
        {
            _renderTarget = renderTarget;
            _gBuffer = new GBuffer();
            _postProcess = new PbrDeferredPostProcess(this);
            Activating.Subscribe(static (sender, ct) =>
            {
                var self = SafeCast.As<DeferredRenderLayer>(sender);
                var screen = self.Screen;
                Debug.Assert(screen is not null);
                self._gBuffer.Initialize(screen);
                self._ppProgram = self._postProcess.Compile(screen);
                return UniTask.CompletedTask;
            });
            Dead.Subscribe(static sender =>
            {
                var self = SafeCast.As<DeferredRenderLayer>(sender);
                self._gBuffer.Dispose();
                self._ppProgram?.Dispose();
                self._ppProgram = null;
            });
        }

        public GBufferData GetGBufferData() => _gBuffer.GetBufferData();

        protected override void OnBeforeExecute(IHostScreen screen, ref FBO currentFbo)
        {
            currentFbo = _gBuffer.FBO;
            FBO.Bind(currentFbo, FBO.Target.FrameBuffer);
            _gBuffer.ClearAllBuffers();
            bool isBlendEnabled = GL.GetInteger(GetPName.Blend) != 0;
            _isBlendEnabledCache = isBlendEnabled;
            if(isBlendEnabled) {
                GL.Disable(EnableCap.Blend);
            }
        }

        protected override void OnAfterExecute(IHostScreen screen, ref FBO currentFbo)
        {
            if(_isBlendEnabledCache) {
                GL.Enable(EnableCap.Blend);
            }
            var gBuffer = _gBuffer;
            var screenSize = screen.FrameBufferSize;
            var gBufSize = gBuffer.Size;
            Debug.Assert(_postProcess is not null);
            Debug.Assert(_ppProgram is not null);
            currentFbo = _renderTarget;
            FBO.Bind(_renderTarget, FBO.Target.FrameBuffer);
            _ppProgram.Render(screenSize, (Vector2)screenSize / (Vector2)gBufSize);
        }

        protected override void OnSizeChanged(IHostScreen screen)
        {
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
}
