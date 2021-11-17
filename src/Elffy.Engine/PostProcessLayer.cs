#nullable enable
using Elffy.Graphics.OpenGL;
using System;
using System.Diagnostics;

namespace Elffy
{
    public abstract class PostProcessLayer : Layer
    {
        protected PostProcessLayer(int sortNumber) : base(sortNumber, 0)
        {
        }

        private protected override void RenderOverride(IHostScreen screen, ref FBO currentFbo)
        {
            var timingPoints = TimingPoints;
            timingPoints.BeforeRendering.DoQueuedEvents();
            if(IsVisible) {
                RenderPostProcess(screen, ref currentFbo);
            }
            timingPoints.AfterRendering.DoQueuedEvents();
        }

        protected abstract void RenderPostProcess(IHostScreen screen, ref FBO currentFbo);

        protected sealed override void OnRendering(IHostScreen screen, ref FBO currentFbo)
        {
            Debug.Fail("This method should not be called.");
            throw new NotSupportedException("This method should not be called.");
        }

        protected sealed override void OnRendered(IHostScreen screen, ref FBO currentFbo)
        {
            Debug.Fail("This method should not be called.");
            throw new NotSupportedException("This method should not be called.");
        }

        protected override void SelectMatrix(IHostScreen screen, out Matrix4 view, out Matrix4 projection)
        {
            Debug.Fail("This method should not be called.");
            throw new NotSupportedException("This method should not be called.");
        }
    }
}
