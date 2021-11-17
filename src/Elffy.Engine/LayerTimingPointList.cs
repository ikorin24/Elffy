#nullable enable
using Elffy.Features.Internal;

namespace Elffy
{
    public sealed class LayerTimingPointList
    {
        private readonly Layer _layer;
        private readonly LayerTimingPoint _beforeRenderingPoint;
        private readonly LayerTimingPoint _afterRenderingPoint;

        internal Layer Layer => _layer;

        public LayerTimingPoint BeforeRendering => _beforeRenderingPoint;
        public LayerTimingPoint AfterRendering => _afterRenderingPoint;

        internal LayerTimingPointList(Layer layer)
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
