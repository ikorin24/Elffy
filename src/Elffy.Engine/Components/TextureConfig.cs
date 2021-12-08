#nullable enable
using System;

namespace Elffy.Components
{
    public struct TextureConfig : IEquatable<TextureConfig>
    {
        public TextureExpansionMode ExpansionMode;
        public TextureShrinkMode ShrinkMode;
        public TextureMipmapMode MipmapMode;
        public TextureWrapMode WrapModeX;
        public TextureWrapMode WrapModeY;

        public TextureConfig(TextureExpansionMode expansionMode,
                             TextureShrinkMode shrinkMode,
                             TextureMipmapMode mipmapMode,
                             TextureWrapMode wrapModeX,
                             TextureWrapMode wrapModeY)
        {
            ExpansionMode = expansionMode;
            ShrinkMode = shrinkMode;
            MipmapMode = mipmapMode;
            WrapModeX = wrapModeX;
            WrapModeY = wrapModeY;
        }

        /// <summary>Get default texture configuration</summary>
        /// <remarks>
        /// ExpansionMode: Bilinear, ShrinkMode: Bilinear, MipmapMode: Bilinear, WrapModeX: ClampToEdge, WrapModeY: ClampToEdge
        /// </remarks>
        public static readonly TextureConfig Default = new()
        {
            ExpansionMode = TextureExpansionMode.Bilinear,
            ShrinkMode = TextureShrinkMode.Bilinear,
            MipmapMode = TextureMipmapMode.Bilinear,
            WrapModeX = TextureWrapMode.ClampToEdge,
            WrapModeY = TextureWrapMode.ClampToEdge,
        };

        public static readonly TextureConfig DefaultNearestNeighbor = new()
        {
            ExpansionMode = TextureExpansionMode.NearestNeighbor,
            ShrinkMode = TextureShrinkMode.NearestNeighbor,
            MipmapMode = TextureMipmapMode.None,
            WrapModeX = TextureWrapMode.ClampToEdge,
            WrapModeY = TextureWrapMode.ClampToEdge,
        };

        public static readonly TextureConfig BilinearRepeat = new()
        {
            ExpansionMode = TextureExpansionMode.Bilinear,
            ShrinkMode = TextureShrinkMode.Bilinear,
            MipmapMode = TextureMipmapMode.Bilinear,
            WrapModeX = TextureWrapMode.Repeat,
            WrapModeY = TextureWrapMode.Repeat,
        };

        public override bool Equals(object? obj) => obj is TextureConfig config && Equals(config);

        public bool Equals(TextureConfig other)
            => ExpansionMode == other.ExpansionMode && ShrinkMode == other.ShrinkMode && MipmapMode == other.MipmapMode &&
               WrapModeX == other.WrapModeX && WrapModeY == other.WrapModeY;

        public override int GetHashCode() => HashCode.Combine(ExpansionMode, ShrinkMode, MipmapMode, WrapModeX, WrapModeY);
    }
}
