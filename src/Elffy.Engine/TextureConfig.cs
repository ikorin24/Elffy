#nullable enable
namespace Elffy;

public record struct TextureConfig(
    TextureExpansionMode ExpansionMode,
    TextureShrinkMode ShrinkMode,
    TextureMipmapMode MipmapMode,
    TextureWrap WrapModeX,
    TextureWrap WrapModeY
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
        WrapModeX = TextureWrap.ClampToEdge,
        WrapModeY = TextureWrap.ClampToEdge,
    };

    public static TextureConfig DefaultNearestNeighbor => new()
    {
        ExpansionMode = TextureExpansionMode.NearestNeighbor,
        ShrinkMode = TextureShrinkMode.NearestNeighbor,
        MipmapMode = TextureMipmapMode.None,
        WrapModeX = TextureWrap.ClampToEdge,
        WrapModeY = TextureWrap.ClampToEdge,
    };

    public static TextureConfig BilinearRepeat => new()
    {
        ExpansionMode = TextureExpansionMode.Bilinear,
        ShrinkMode = TextureShrinkMode.Bilinear,
        MipmapMode = TextureMipmapMode.Bilinear,
        WrapModeX = TextureWrap.Repeat,
        WrapModeY = TextureWrap.Repeat,
    };
}
