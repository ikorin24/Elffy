#nullable enable
using Elffy.Components;
using Elffy.Imaging;
using System;
using System.IO;

namespace Elffy
{
    public static class ResourceStreamExtension
    {
        /// <summary>Create <see cref="Texture"/> from <see cref="Stream"/>; (Stream is data as an image file, not pixel raw data.)</summary>
        /// <remarks>Created <see cref="Texture"/> expands and shrinks linearly, and has no mipmap.</remarks>
        /// <param name="stream">source stream</param>
        /// <param name="bitmapType">image file type</param>
        /// <param name="disposeStream">dispose stream if true. (default is true)</param>
        /// <returns><see cref="Texture"/> created from <see cref="Stream"/></returns>
        public static Texture ToTexture(this Stream stream, BitmapType bitmapType, bool disposeStream = true)
        {
            return ToTexture(stream, bitmapType, 
                             TextureExpansionMode.Bilinear,
                             TextureShrinkMode.Bilinear,
                             TextureMipmapMode.None,
                             disposeStream);
        }

        /// <summary>Create <see cref="Texture"/> from <see cref="Stream"/>; (Stream is data as an image file, not pixel raw data.)</summary>
        /// <param name="stream">source stream</param>
        /// <param name="bitmapType">image file type</param>
        /// <param name="expansionMode">texture expansion mode</param>
        /// <param name="shrinkMode">textrue shrink mode</param>
        /// <param name="mipmapMode">texture mipmap mode</param>
        /// <param name="disposeStream">dispose stream if ture. (default is true)</param>
        /// <returns><see cref="Texture"/> create from <see cref="Stream"/></returns>
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
