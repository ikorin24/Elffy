#nullable enable

using Elffy.Graphics.OpenGL;
using System;

namespace Elffy
{
    public class WorldLayer : Layer, IObjectLayer
    {
        private const int DefaultSortNumber = 0;

        public WorldLayer(int sortNumber = DefaultSortNumber) : base(sortNumber)
        {
        }

        public ReadOnlySpan<FrameObject> GetFrameObjects() => Objects;

        protected override void OnAlive(IHostScreen screen)
        {
            // nop
        }

        protected override void OnDead()
        {
            // nop
        }

        protected override void OnRendered(IHostScreen screen, ref FBO currentFbo)
        {
            // nop
        }

        protected override void OnRendering(IHostScreen screen, ref FBO currentFbo)
        {
            FBO.Bind(FBO.Empty, FBO.Target.FrameBuffer);
            currentFbo = FBO.Empty;
        }

        protected override void OnSizeChanged(IHostScreen screen)
        {
            // nop
        }
    }

    public interface IObjectLayer
    {
        ReadOnlySpan<FrameObject> GetFrameObjects();
    }
}
