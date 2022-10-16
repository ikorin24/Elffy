#nullable enable
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Elffy.Graphics.OpenGL;
using Elffy.Shading;

namespace Elffy
{
    public sealed class PostProcessOperation : PipelineOperation
    {
        private const int DefaultSortNumber = 1000;

        private readonly FBO _renderTarget;
        private readonly PostProcess? _postProcess;
        private PostProcessProgram? _ppProgram;

        public PostProcess? PostProcess
        {
            get => _postProcess;
            init => _postProcess = value;
        }

        public PostProcessOperation(int sortNumber = DefaultSortNumber) : this(FBO.Empty, null, sortNumber) { }

        public PostProcessOperation(string? name, int sortNumber = DefaultSortNumber) : this(FBO.Empty, name, sortNumber) { }
        public PostProcessOperation(FBO renderTarget, int sortNumber = DefaultSortNumber) : this(renderTarget, null, sortNumber) { }

        public PostProcessOperation(FBO renderTarget, string? name, int sortNumber = DefaultSortNumber) : base(sortNumber, name)
        {
            _renderTarget = renderTarget;
            Activating.Subscribe(static (sender, ct) =>
            {
                var self = SafeCast.As<PostProcessOperation>(sender);
                var screen = self.Screen;
                Debug.Assert(screen is not null);
                var postProcess = self._postProcess;
                if(postProcess != null) {
                    self._ppProgram = postProcess.Compile(screen);
                }
                return UniTask.CompletedTask;
            });
            Dead.Subscribe(static sender =>
            {
                var self = SafeCast.As<PostProcessOperation>(sender);
                self._ppProgram?.Dispose();
                self._ppProgram = null;
            });
        }

        protected override void OnAfterExecute(IHostScreen screen, ref FBO currentFbo)
        {
        }


        protected override void OnBeforeExecute(IHostScreen screen, ref FBO currentFbo)
        {
            currentFbo = _renderTarget;
            FBO.Bind(currentFbo, FBO.Target.FrameBuffer);
        }

        protected override void OnExecute(IHostScreen screen)
        {
            var context = new PostProcessRenderContext(screen, this);
            _ppProgram?.Render(in context);
        }

        protected override void OnSizeChanged(IHostScreen screen)
        {
        }
    }
}
