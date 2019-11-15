#nullable enable

namespace Elffy
{
    /// <summary>テクスチャの拡大モード</summary>
    public enum TextureExpansionMode
    {
        /// <summary>線形補間</summary>
        Bilinear,
        /// <summary>最近傍補間</summary>
        NearestNeighbor,
    }

    /// <summary>テクスチャの縮小モード</summary>
    public enum TextureShrinkMode
    {
        /// <summary>線形補間</summary>
        Bilinear,
        /// <summary>最近傍補間</summary>
        NearestNeighbor,
    }

    /// <summary>テクスチャのミップマップモード</summary>
    public enum TextureMipmapMode
    {
        /// <summary>ミップマップを使用しません</summary>
        None,
        /// <summary>線形補間</summary>
        Bilinear,
        /// <summary>最近傍補間</summary>
        NearestNeighbor,
    }
}
