#nullable enable
using Elffy.Graphics.OpenGL;

namespace Elffy
{
    public sealed class ForwardRenderLayer : ObjectLayer
    {
        private const int DefaultSortNumber = 0;
        private readonly FBO _renderTarget;

        public ForwardRenderLayer(int sortNumber = DefaultSortNumber) : this(FBO.Empty, sortNumber)
        {
        }

        public ForwardRenderLayer(FBO renderTarget, int sortNumber = DefaultSortNumber) : base(sortNumber)
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
