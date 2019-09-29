using Elffy.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    public class Layer
    {
        private readonly FrameObjectStore _objectStore = new FrameObjectStore();

        public string Name { get; set; }

        public RenderingMode RenderingMode { get; set; }

        public Layer(RenderingMode renderingMode) : this(null, renderingMode) { }

        public Layer(string name, RenderingMode renderingMode)
        {
            Name = name;
            RenderingMode = renderingMode;
        }
    }

    public enum RenderingMode
    {
        Perspective,
        Orthographic,
    }
}
