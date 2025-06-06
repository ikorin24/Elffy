﻿#nullable enable
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
        private bool _isBlendEnabledCache;

        public DeferredRenderLayer(int sortNumber = DefaultSortNumber) : this(FBO.Empty, null, sortNumber) { }

        public DeferredRenderLayer(string? name, int sortNumber = DefaultSortNumber) : this(FBO.Empty, name, sortNumber) { }

        public DeferredRenderLayer(FBO renderTarget, int sortNumber = DefaultSortNumber) : this(renderTarget, null, sortNumber) { }

        public DeferredRenderLayer(FBO renderTarget, string? name, int sortNumber = DefaultSortNumber) : base(sortNumber, name)
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
                self._postProcess.InvokeAttached();
                return UniTask.CompletedTask;
            });
            Dead.Subscribe(static sender =>
            {
                var self = SafeCast.As<DeferredRenderLayer>(sender);
                self._postProcess.InvokeDetached();
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
            Debug.Assert(_postProcess is not null);
            Debug.Assert(_ppProgram is not null);
            currentFbo = _renderTarget;
            FBO.Bind(_renderTarget, FBO.Target.FrameBuffer);
            var context = new PostProcessRenderContext(screen, this);
            var uvScale = (Vector2)screen.FrameBufferSize / (Vector2)gBuffer.Size;
            _ppProgram.Render(in context, in uvScale);
        }

        protected override void OnSizeChanged(IHostScreen screen)
        {
            var timing = screen.Timings.FrameInitializing;
            timing.Post(static x =>
            {
                var gBuffer = SafeCast.NotNullAs<GBuffer>(x);
                gBuffer.Resize();
            }, _gBuffer);
        }
    }
}
