#nullable enable
using Elffy.Features.Internal;

namespace Elffy
{
    public sealed class LayerTimingPointList
    {
        private readonly ILayer _layer;
        private readonly LayerTimingPoint _beforeRenderingPoint;
        private readonly LayerTimingPoint _afterRenderingPoint;

        internal ILayer Layer => _layer;

        public LayerTimingPoint BeforeRendering => _beforeRenderingPoint;
        public LayerTimingPoint AfterRendering => _afterRenderingPoint;

        internal LayerTimingPointList(ILayer layer)
        {
            _layer = layer;
            _beforeRenderingPoint = new LayerTimingPoint(layer);
            _afterRenderingPoint = new LayerTimingPoint(layer);
        }

        internal void AbortAllEvents()
        {
            _beforeRenderingPoint.AbortAllEvents();
            _afterRenderingPoint.AbortAllEvents();
        }
    }
}
