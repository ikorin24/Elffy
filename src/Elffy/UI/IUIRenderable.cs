#nullable enable

namespace Elffy.UI
{
    /// <summary>UI 要素を描画するためのインターフェース</summary>
    internal interface IUIRenderable
    {
        /// <summary>このオブジェクトの描画対象である論理 UI コントロール</summary>
        Control Control { get; }
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
