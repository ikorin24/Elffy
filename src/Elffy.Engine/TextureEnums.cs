#nullable enable
namespace Elffy;

/// <summary>Texture expansion mode</summary>
public enum TextureExpansionMode : byte
{
    /// <summary>bilinear interpolation</summary>
    Bilinear = 0,
    /// <summary>nearest neighbor interpolation</summary>
    NearestNeighbor,
}

/// <summary>Texture shrink mode</summary>
public enum TextureShrinkMode : byte
{
    /// <summary>bilinear interpolation</summary>
    Bilinear = 0,
    /// <summary>nearest neighbor interpolation</summary>
    NearestNeighbor,
}

/// <summary>texture mipmap mode</summary>
public enum TextureMipmapMode : byte
{
    /// <summary>bilinear interpolation</summary>
    Bilinear = 0,
    /// <summary>nearest neighbor interpolation</summary>
    NearestNeighbor,
    /// <summary>not use mipmap</summary>
    None,
}

/// <summary>Texture wrap mode</summary>
public enum TextureWrapMode : byte
{
    /// <summary>clamp to edge</summary>
    ClampToEdge = 0,
    /// <summary>repeat</summary>
    Repeat,
    /// <summary>mirrored repeat</summary>
    MirroredRepeat,
    /// <summary>clamp to border</summary>
    ClampToBorder,
}
