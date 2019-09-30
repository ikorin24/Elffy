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
        internal FrameObjectStore ObjectStore { get; private set; } = new FrameObjectStore();

        public string Name { get; set; }

        public RenderingMode RenderingMode { get; set; }

        public Layer(RenderingMode renderingMode) : this(null, renderingMode) { }

        public Layer(string name, RenderingMode renderingMode)
        {
            Name = name;
            RenderingMode = renderingMode;
        }

        public void AddFrameObject(FrameObject frameObject) => ObjectStore.AddFrameObject(frameObject);

        public bool RemoveFrameObject(FrameObject frameObject) => ObjectStore.RemoveFrameObject(frameObject);

        public FrameObject FindObject(string tag) => ObjectStore.FindObject(tag);

        public List<FrameObject> FindAllObject(string tag) => ObjectStore.FindAllObject(tag);

        public void Clear() => ObjectStore.Clear();
    }

    public enum RenderingMode
    {
        Perspective,
        Orthographic,
    }
}
