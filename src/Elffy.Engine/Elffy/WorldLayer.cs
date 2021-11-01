#nullable enable

namespace Elffy
{
    public class WorldLayer : Layer
    {
        private const int DefaultSortNumber = 0;

        public WorldLayer(int sortNumber = DefaultSortNumber) : base(sortNumber)
        {
        }

        protected override void OnAlive(IHostScreen screen)
        {
            // nop
        }

        protected override void OnLayerTerminated()
        {
            // nop
        }

        protected override void OnRendered(IHostScreen screen)
        {
            // nop
        }

        protected override void OnRendering(IHostScreen screen)
        {
            // nop
        }

        protected override void OnSizeChanged(IHostScreen screen)
        {
            // nop
        }
    }
}
