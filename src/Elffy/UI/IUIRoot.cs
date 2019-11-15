#nullable enable

namespace Elffy.UI
{
    /// <summary>UI の論理構造の Root 要素を表すインターフェース</summary>
    public interface IUIRoot
    {
        /// <summary>この <see cref="IUIRoot"/> とその子孫を描画するための UI 描画レイヤー</summary>
        Layer UILayer { get; }

        /// <summary>この <see cref="IUIRoot"/> の UI tree 構造の子供</summary>
        ControlCollection Children { get; }
    }
}
