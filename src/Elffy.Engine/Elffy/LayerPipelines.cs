#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.UI;

namespace Elffy
{
    public static class LayerPipelines
    {
        public static UniTask<(DeferredRenderingLayer, WorldLayer, UILayer)> DefaultDeferredRendering(IHostScreen screen)
        {
            return UniTask.WhenAll(
                new DeferredRenderingLayer(1).Activate(screen),
                new WorldLayer().Activate(screen),
                new UILayer().Activate(screen));
        }

        public static UniTask<(WorldLayer, UILayer)> DefaultForwardRendering(IHostScreen screen)
        {
            return UniTask.WhenAll(
                new WorldLayer().Activate(screen),
                new UILayer().Activate(screen));
        }
    }
}
