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
        /// <summary>このレイヤーのライティングを有効にするかどうか</summary>
        public bool IsLightingEnabled { get; set; }

        /// <summary>レイヤー名を指定して <see cref="Layer"/> を作成します</summary>
        /// <param name="name">レイヤー名</param>
        public Layer(string name)
        {
            Name = name;
        }
    }
}
