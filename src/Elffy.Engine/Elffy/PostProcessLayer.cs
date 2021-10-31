#nullable enable
using System;
using System.Diagnostics;

namespace Elffy
{
    public abstract class PostProcessLayer : Layer
    {
        protected PostProcessLayer(string name, int sortNumber) : base(name, sortNumber, 0)
        {
        }

        private protected override void RenderOverride(IHostScreen screen)
        {
            var timingPoints = TimingPoints;
            timingPoints.BeforeRendering.DoQueuedEvents();
            if(IsVisible) {
                RenderPostProcess(screen);
            }
            timingPoints.AfterRendering.DoQueuedEvents();
        }

        protected abstract void RenderPostProcess(IHostScreen screen);

        protected sealed override void OnRendering(IHostScreen screen)
        {
            Debug.Fail("This method should not be called.");
            throw new NotSupportedException("This method should not be called.");
        }

        protected sealed override void OnRendered(IHostScreen screen)
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
