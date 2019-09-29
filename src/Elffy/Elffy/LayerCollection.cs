using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    public class LayerCollection
    {
        private const string UI_LAYER_NAME = "UILayer";
        private const string WORLD_LAYER_NAME = "WorldLayer";
        private readonly List<Layer> _list = new List<Layer>();

        public Layer UILayer { get; }
        public Layer WorldLayer { get; }

        internal LayerCollection()
        {
            UILayer = new Layer(UI_LAYER_NAME, RenderingMode.Orthographic);
            WorldLayer = new Layer(WORLD_LAYER_NAME, RenderingMode.Perspective);
            AddDefaltLayers();
        }

        public void Add(Layer layer)
        {
            if(layer == null) { throw new ArgumentNullException(nameof(layer)); }
            throw new NotImplementedException();
        }

        private bool Remove(Layer layer)
        {
            if(layer == null) { throw new ArgumentNullException(nameof(layer)); }
            if(layer == UILayer || layer == WorldLayer) {
                return false;
            }
            return _list.Remove(layer);
        }

        public void Clear()
        {
            _list.Clear();
            AddDefaltLayers();
        }

        private void AddDefaltLayers()
        {
            _list.Add(UILayer);
            _list.Add(WorldLayer);
        }
    }
}
