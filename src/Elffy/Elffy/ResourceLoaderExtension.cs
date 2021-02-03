#nullable enable
using Elffy.Components;
using Elffy.Imaging;
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;

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
                              TextureMipmapMode.Bilinear,
                              TextureWrapMode.ClampToEdge,
                              TextureWrapMode.ClampToEdge);
        }

        /// <summary>Create <see cref="Texture"/> from resource</summary>
        /// <param name="name">resource name</param>
        /// <param name="bitmapType">image file type</param>
        /// <param name="expansionMode">texture expansion mode</param>
        /// <param name="shrinkMode">textrue shrink mode</param>
        /// <param name="mipmapMode">texture mipmap mode</param>
        /// <param name="wrapModeX">texture x wrap mode</param>
        /// <param name="wrapModeY">texture y wrap mode</param>
        /// <returns><see cref="Texture"/> create from <see cref="Stream"/></returns>
        public static Texture LoadTexture(this IResourceLoader source, string name, BitmapType bitmapType, TextureExpansionMode expansionMode,
                                          TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode,
                                          TextureWrapMode wrapModeX, TextureWrapMode wrapModeY)
        {
            using(var stream = source.GetStream(name))
            using(var bitmap = BitmapHelper.StreamToBitmap(stream, bitmapType)) {
                var texture = new Texture(expansionMode, shrinkMode, mipmapMode, wrapModeX, wrapModeY);
                texture.Load(bitmap);
                return texture;
            }
        }

        public static UniTask<Texture> LoadTextureAsync(this IResourceLoader source, string name, BitmapType bitmapType,
                                                        AsyncBackEndPoint endPoint,
                                                        FrameLoopTiming timing,
                                                        CancellationToken cancellationToken = default)
        {
            return LoadTextureAsync(source, name, bitmapType,
                                    TextureExpansionMode.Bilinear,
                                    TextureShrinkMode.Bilinear,
                                    TextureMipmapMode.Bilinear,
                                    TextureWrapMode.ClampToEdge,
                                    TextureWrapMode.ClampToEdge, endPoint, timing, cancellationToken);
        }

        public static async UniTask<Texture> LoadTextureAsync(this IResourceLoader source, string name, BitmapType bitmapType, TextureExpansionMode expansionMode,
                                                              TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode,
                                                              TextureWrapMode wrapModeX, TextureWrapMode wrapModeY,
                                                              AsyncBackEndPoint endPoint,
                                                              FrameLoopTiming timing,
                                                              CancellationToken cancellationToken = default)
        {
            if(endPoint is null) {
                throw new ArgumentNullException(nameof(endPoint));
            }
            timing.ThrowArgExceptionIfInvalid(nameof(timing));
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.SwitchToThreadPool();
            cancellationToken.ThrowIfCancellationRequested();

            using var stream = source.GetStream(name);
            using var bitmap = BitmapHelper.StreamToBitmap(stream, bitmapType);
            await endPoint.ToTiming(timing, cancellationToken);
            var texture = new Texture(expansionMode, shrinkMode, mipmapMode, wrapModeX, wrapModeY);
            texture.Load(bitmap);
            return texture;
        }

        public static Typeface LoadTypeface(this IResourceLoader source, string name)
        {
            using var stream = source.GetStream(name);
            return new Typeface(stream);
        }
    }
}
