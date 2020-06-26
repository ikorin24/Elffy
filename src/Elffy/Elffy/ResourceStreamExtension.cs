#nullable enable
using Elffy.Components;
using Elffy.Imaging;
using System;
using System.IO;

namespace Elffy
{
    public static class ResourceStreamExtension
    {
        public static Texture ToTexture(this Stream stream, BitmapType bitmapType)
            => ToTexture(stream, bitmapType, TextureExpansionMode.Bilinear, TextureShrinkMode.NearestNeighbor, TextureMipmapMode.None);

        public static Texture ToTexture(this Stream stream, BitmapType bitmapType, TextureExpansionMode expansionMode, TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode)
        {
            if(stream is null) { throw new ArgumentNullException(nameof(stream)); }
            using(var bitmap = BitmapHelper.StreamToBitmap(stream, bitmapType)) {
                var texture = new Texture(expansionMode, shrinkMode, mipmapMode);
                texture.Load(bitmap);
                return texture;
            }
        }
    }
}
