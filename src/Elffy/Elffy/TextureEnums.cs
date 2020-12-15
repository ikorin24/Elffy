#nullable enable

namespace Elffy
{
    /// <summary>Texture expansion mode</summary>
    public enum TextureExpansionMode
    {
        /// <summary>bilinear interpolation</summary>
        Bilinear,
        /// <summary>nearest neighbor interpolation</summary>
        NearestNeighbor,
    }

    /// <summary>Texture shrink mode</summary>
    public enum TextureShrinkMode
    {
        /// <summary>bilinear interpolation</summary>
        Bilinear,
        /// <summary>nearest neighbor interpolation</summary>
        NearestNeighbor,
    }

    /// <summary>texture mipmap mode</summary>
    public enum TextureMipmapMode
    {
        /// <summary>bilinear interpolation</summary>
        Bilinear,
        /// <summary>nearest neighbor interpolation</summary>
        NearestNeighbor,
        /// <summary>not use mipmap</summary>
        None,
    }

    /// <summary>Texture wrap mode</summary>
    public enum TextureWrapMode
    {
        /// <summary>clamp to edge</summary>
        ClampToEdge,
        /// <summary>repeat</summary>
        Repeat,
        /// <summary>mirrored repeat</summary>
        MirroredRepeat,
        /// <summary>clamp to border</summary>
        ClampToBorder,
    }
}
