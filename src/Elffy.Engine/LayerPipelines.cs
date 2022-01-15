#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.UI;

namespace Elffy
{
    public static class LayerPipelines
    {
        public static UniTask<(WorldLayer, UILayer)> BuildDefault(IHostScreen screen)
        {
            return CreateBuilder(screen)
                .Build(() => new WorldLayer(),
                       () => new UILayer());
        }

        public static LayerPipelineBuilder CreateBuilder(IHostScreen screen) => new LayerPipelineBuilder(screen);
    }
}
