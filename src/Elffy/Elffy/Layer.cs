using Elffy.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    /// <summary><see cref="FrameObject"/> のレイヤークラス</summary>
    public class Layer : LayerBase
    {
        /// <summary>このレイヤーの描画モード</summary>
        public ProjectionMode RenderingMode { get; private set; }

        /// <summary>投影モードを指定して <see cref="Layer"/> を作成します</summary>
        /// <param name="renderingMode">このレイヤーの投影モード</param>
        public Layer(ProjectionMode renderingMode) : this(null, renderingMode) { }

        /// <summary>レイヤー名と投影モードを指定して <see cref="Layer"/> を作成します</summary>
        /// <param name="name">レイヤー名</param>
        /// <param name="renderingMode">投影モード</param>
        public Layer(string name, ProjectionMode renderingMode)
        {
            Name = name;
            RenderingMode = renderingMode;
        }
    }
}
