#nullable enable
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Elffy
{
    public class WorldLayer : Layer
    {
        private const int DefaultSortNumber = 0;

        public WorldLayer(string name, int sortNumber = DefaultSortNumber) : base(name, sortNumber)
        {
        }

        public static UniTask<WorldLayer> NewActivate(IHostScreen screen, string name, int sortNumber = DefaultSortNumber, CancellationToken cancellationToken = default)
        {
            return new WorldLayer(name, sortNumber).Activate(screen, cancellationToken);
        }

        public static UniTask<WorldLayer> NewActivate(IHostScreen screen, string name, FrameTimingPoint timingPoint, int sortNumber = DefaultSortNumber, CancellationToken cancellationToken = default)
        {
            return new WorldLayer(name, sortNumber).Activate(screen, timingPoint, cancellationToken);
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
