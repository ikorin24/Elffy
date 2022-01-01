#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.UI;

namespace Elffy
{
    public static class LayerPipelines
    {
        public static UniTask<(DeferredRenderingLayer, WorldLayer, UILayer)> UseDeferredForward(IHostScreen screen)
        {
            return UniTask.WhenAll(
                new DeferredRenderingLayer().Activate(screen),
                new WorldLayer().Activate(screen),
                new UILayer().Activate(screen));
        }

        public static UniTask<(WorldLayer, UILayer)> UseForward(IHostScreen screen)
        {
            return UniTask.WhenAll(
                new WorldLayer().Activate(screen),
                new UILayer().Activate(screen));
        }
    }
}
