#nullable enable

namespace Elffy.Core
{
    /// <summary><see cref="FrameObject"/> を所持するレイヤーの機能を提供するクラス</summary>
    public abstract class LayerBase : FrameObjectStore
    {
        /// <summary>レイヤーの名前</summary>
        public string Name { get; set; } = string.Empty;
    }
}
