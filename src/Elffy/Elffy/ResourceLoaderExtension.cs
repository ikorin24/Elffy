#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Components;
using Elffy.Effective;
using Elffy.Imaging;
using Elffy.Serialization;
using Elffy.Shapes;
using MMDTools.Unmanaged;
using System;
using System.Drawing;
using System.IO;

namespace Elffy
{
    public static class ResourceLoaderExtension
    {
        /// <summary>Create <see cref="Texture"/> from resource</summary>
        /// <remarks>Created <see cref="Texture"/> expands and shrinks linearly, and has no mipmap.</remarks>
        /// <param name="name">resource name</param>
        /// <param name="bitmapType">image file type</param>
        /// <returns><see cref="Texture"/> created from <see cref="Stream"/></returns>
        public static Texture LoadTexture(this IResourceLoader source, string name, BitmapType bitmapType)
        {
            return LoadTexture(source, name, bitmapType,
                             TextureExpansionMode.Bilinear,
                             TextureShrinkMode.Bilinear,
                             TextureMipmapMode.None);
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

        public static UniTask<Texture> LoadTextureAsync(this IResourceLoader source, string name, BitmapType bitmapType)
        {
            return UniTask.Run(Load, false);

            Texture Load() => LoadTexture(source, name, bitmapType);
        }

        public static UniTask<Texture> LoadTextureAsync(this IResourceLoader source, string name, BitmapType bitmapType, TextureExpansionMode expansionMode,
                                        TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode)
        {
            return UniTask.Run(Load, false);

            Texture Load() => LoadTexture(source, name, bitmapType, expansionMode, shrinkMode, mipmapMode);
        }

        public static UniTask<PmxModel> LoadPmxModelAsync(this IResourceLoader source, string name)
        {
            return UniTask.Run(Load, false);

            PmxModel Load() => LoadPmxModel(source, name);
        }

        public static PmxModel LoadPmxModel(this IResourceLoader source, string name)
        {
            PMXObject? pmx = default;
            try {
                using(var stream = source.GetStream(name)) {
                    pmx = PMXParser.Parse(stream);
                }
                var textureNames = pmx.TextureList.AsSpan();
                var dir = ResourcePath.GetDirectoryName(name);
                var bitmaps = new RefTypeRentMemory<Bitmap>(textureNames.Length);
                var bitmapSpan = bitmaps.Span;

                for(int i = 0; i < bitmapSpan.Length; i++) {
                    using(GetTexturePath(dir, textureNames[i], out var texturePath, out var ext))
                    using(var stream = source.GetStream(texturePath.ToString())) {
                        bitmapSpan[i] = BitmapHelper.StreamToBitmap(stream, ext);
                    }
                }
                return new PmxModel(pmx, bitmaps);
            }
            catch {
                pmx?.Dispose();
                throw;
            }

            #region local func
            static PooledArray<char> GetTexturePath(ReadOnlySpan<char> dir, ReadOnlyRawString name, out Span<char> texturePath, out ReadOnlySpan<char> ext)
            {
                var pooledArray = new PooledArray<char>(dir.Length + 1 + name.GetCharCount());
                try {
                    texturePath = pooledArray.AsSpan();
                    dir.CopyTo(texturePath);
                    texturePath[dir.Length] = '/';
                    name.ToString(texturePath.Slice(dir.Length + 1));
                    texturePath.Replace('\\', '/');
                    ext = ResourcePath.GetExtension(texturePath);

                    return pooledArray;
                }
                catch {
                    pooledArray.Dispose();
                    throw;
                }
            }
            #endregion

        }

        public static UniTask<Model3D> LoadFbxModelAsync(this IResourceLoader source, string name)
        {
            return UniTask.Run(Load, false);

            Model3D Load() => LoadFbxModel(source, name);
        }

        public static Model3D LoadFbxModel(this IResourceLoader source, string name)
        {
            using(var stream = source.GetStream(name)) {
                var (vertices, indices) = FbxModelBuilder.LoadModel(stream);
                return new Model3D(vertices, indices);
            }
        }
    }
}
