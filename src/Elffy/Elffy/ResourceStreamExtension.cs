#nullable enable
using Elffy.Components;
using Elffy.Imaging;
using System;
using System.Drawing;
using System.IO;

namespace Elffy
{
    public static class ResourceStreamExtension
    {
        public static Texture ToTexture(this Stream stream, BitmapType bitmapType)
            => ToTexture(stream, bitmapType, TextureExpansionMode.Bilinear, TextureShrinkMode.Bilinear, TextureMipmapMode.None);

        public static Texture ToTexture(this Stream stream, BitmapType bitmapType, TextureExpansionMode expansionMode,
                                        TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode,
                                        bool disposeStream = true)
        {
            if(stream is null) { throw new ArgumentNullException(nameof(stream)); }
            try {
                using(var bitmap = BitmapHelper.StreamToBitmap(stream, bitmapType)) {
                    var texture = new Texture(expansionMode, shrinkMode, mipmapMode);
                    texture.Load(bitmap);
                    return texture;
                }
            }
            finally {
                if(disposeStream) {
                    stream.Dispose();
                }
            }
        }
    }
}
