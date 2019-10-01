using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.UI
{
    /// <summary>UI 要素を描画するためのインターフェース</summary>
    internal interface IUIRenderable
    {
        /// <summary>このオブジェクトが描画されるかどうかを取得します</summary>
        bool IsVisible { get; }

        /// <summary>このオブジェクトを描画します</summary>
        void Render();

        /// <summary>このオブジェクトを開始します</summary>
        void Activate();

        /// <summary>このオブジェクトを停止します</summary>
        void Destroy();
    }
}
