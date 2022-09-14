#nullable enable
using Elffy.Graphics.OpenGL;

namespace Elffy
{
    public sealed class ForwardRenderLayer : ObjectLayer
    {
        private FBO _renderTarget;

        public ForwardRenderLayer(int sortNumber) : this(FBO.Empty, sortNumber)
        {
        }

        public ForwardRenderLayer(FBO renderTarget, int sortNumber) : base(sortNumber)
        {
            _renderTarget = renderTarget;
        }

        protected override void OnRendered(IHostScreen screen, ref FBO currentFbo)
        {
            // nop
        }

        protected override void OnRendering(IHostScreen screen, ref FBO currentFbo)
        {
            FBO.Bind(_renderTarget, FBO.Target.FrameBuffer);
            currentFbo = _renderTarget;
        }

        protected override void OnSizeChanged(IHostScreen screen)
        {
            // nop
        }

        protected override void SelectMatrix(IHostScreen screen, out Matrix4 view, out Matrix4 projection)
        {
            var camera = screen.Camera;
            view = camera.View;
            projection = camera.Projection;
        }
    }
}
