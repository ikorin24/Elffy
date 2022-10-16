#nullable enable
using Elffy.Graphics.OpenGL;

namespace Elffy
{
    public sealed class ForwardRenderLayer : ObjectLayer
    {
        private const int DefaultSortNumber = 0;
        private readonly FBO _renderTarget;

        public ForwardRenderLayer(int sortNumber = DefaultSortNumber) : this(FBO.Empty, null, sortNumber) { }
        public ForwardRenderLayer(string? name, int sortNumber = DefaultSortNumber) : this(FBO.Empty, name, sortNumber) { }
        public ForwardRenderLayer(FBO renderTarget, int sortNumber = DefaultSortNumber) : this(renderTarget, null, sortNumber) { }

        public ForwardRenderLayer(FBO renderTarget, string? name, int sortNumber = DefaultSortNumber) : base(sortNumber, name)
        {
            _renderTarget = renderTarget;
        }

        protected override void OnAfterExecute(IHostScreen screen, ref FBO currentFbo)
        {
            // nop
        }

        protected override void OnBeforeExecute(IHostScreen screen, ref FBO currentFbo)
        {
            FBO.Bind(_renderTarget, FBO.Target.FrameBuffer);
            currentFbo = _renderTarget;
        }

        protected override void OnSizeChanged(IHostScreen screen)
        {
            // nop
        }
    }
}
