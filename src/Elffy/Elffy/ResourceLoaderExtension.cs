#nullable enable
using Elffy.Components;
using Elffy.Imaging;
using System.IO;

namespace Elffy
{
    public static class ResourceLoaderExtension
    {
        /// <summary>Create <see cref="Texture"/> from resource</summary>
        /// <remarks>Created <see cref="Texture"/> expands and shrinks linearly.</remarks>
        /// <param name="name">resource name</param>
        /// <param name="bitmapType">image file type</param>
        /// <returns><see cref="Texture"/> created from <see cref="Stream"/></returns>
        public static Texture LoadTexture(this IResourceLoader source, string name, BitmapType bitmapType)
        {
            return LoadTexture(source, name, bitmapType,
                             TextureExpansionMode.Bilinear,
                             TextureShrinkMode.Bilinear,
                             TextureMipmapMode.Bilinear);
        }

        /// <summary>Create <see cref="Texture"/> from resource</summary>
        /// <param name="name">resource name</param>
        /// <param name="bitmapType">image file type</param>
        /// <param name="expansionMode">texture expansion mode</param>
        /// <param name="shrinkMode">textrue shrink mode</param>
        /// <param name="mipmapMode">texture mipmap mode</param>
        /// <returns><see cref="Texture"/> create from <see cref="Stream"/></returns>
        public static Texture LoadTexture(this IResourceLoader source, string name, BitmapType bitmapType, TextureExpansionMode expansionMode,
                                        TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode)
        {
            using(var stream = source.GetStream(name))
            using(var bitmap = BitmapHelper.StreamToBitmap(stream, bitmapType)) {
                var texture = new Texture(expansionMode, shrinkMode, mipmapMode);
                texture.Load(bitmap);
                return texture;
            }
        }

        public static Typeface LoadTypeface(this IResourceLoader source, string name)
        {
            using var stream = source.GetStream(name);
            return new Typeface(stream);
        }
    }
}
