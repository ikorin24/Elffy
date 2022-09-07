#nullable enable
namespace Elffy;

public record struct TextureConfig(
    TextureExpansionMode ExpansionMode,
    TextureShrinkMode ShrinkMode,
    TextureMipmapMode MipmapMode,
    TextureWrapMode WrapModeX,
    TextureWrapMode WrapModeY
)
{
    /// <summary>Get default texture configuration</summary>
    /// <remarks>
    /// ExpansionMode: Bilinear, ShrinkMode: Bilinear, MipmapMode: Bilinear, WrapModeX: ClampToEdge, WrapModeY: ClampToEdge
    /// </remarks>
    public static TextureConfig Default => new()
    {
        ExpansionMode = TextureExpansionMode.Bilinear,
        ShrinkMode = TextureShrinkMode.Bilinear,
        MipmapMode = TextureMipmapMode.Bilinear,
        WrapModeX = TextureWrapMode.ClampToEdge,
        WrapModeY = TextureWrapMode.ClampToEdge,
    };

    public static TextureConfig DefaultNearestNeighbor => new()
    {
        ExpansionMode = TextureExpansionMode.NearestNeighbor,
        ShrinkMode = TextureShrinkMode.NearestNeighbor,
        MipmapMode = TextureMipmapMode.None,
        WrapModeX = TextureWrapMode.ClampToEdge,
        WrapModeY = TextureWrapMode.ClampToEdge,
    };

    public static TextureConfig BilinearRepeat => new()
    {
        ExpansionMode = TextureExpansionMode.Bilinear,
        ShrinkMode = TextureShrinkMode.Bilinear,
        MipmapMode = TextureMipmapMode.Bilinear,
        WrapModeX = TextureWrapMode.Repeat,
        WrapModeY = TextureWrapMode.Repeat,
    };
}
