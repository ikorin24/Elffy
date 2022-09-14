#nullable enable
using System;
using Elffy.Graphics.OpenGL;

namespace Elffy
{
    public sealed class DeferredRenderLayer : ObjectLayer
    {
        public DeferredRenderLayer(int sortNumber) : base(sortNumber)
        {
        }

        protected override void OnRendered(IHostScreen screen, ref FBO currentFbo)
        {
            throw new NotImplementedException();
        }

        protected override void OnRendering(IHostScreen screen, ref FBO currentFbo)
        {
            throw new NotImplementedException();
        }

        protected override void OnSizeChanged(IHostScreen screen)
        {
            throw new NotImplementedException();
        }

        protected override void SelectMatrix(IHostScreen screen, out Matrix4 view, out Matrix4 projection)
        {
            throw new NotImplementedException();
        }
    }
}
