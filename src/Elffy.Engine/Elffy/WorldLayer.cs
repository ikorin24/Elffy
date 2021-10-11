#nullable enable

using Cysharp.Threading.Tasks;
using System.Threading;

namespace Elffy
{
    public sealed class WorldLayer : Layer
    {
        public const int DefaultSortNumber = 0;

        public WorldLayer(string name, int sortNumber = DefaultSortNumber) : base(name, sortNumber)
        {
        }

        public static UniTask<WorldLayer> NewActivate(IHostScreen screen, string name, int sortNumber = DefaultSortNumber, CancellationToken cancellationToken = default)
        {
            return new WorldLayer(name, sortNumber).Activate(screen, cancellationToken);
        }

        protected override void OnAlive(IHostScreen screen)
        {
            // nop
        }

        protected override void OnLayerTerminated()
        {
            // nop
        }

        protected override void OnSizeChanged(IHostScreen screen)
        {
            // nop
        }
    }
}
