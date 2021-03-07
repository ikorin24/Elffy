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
        /// <returns><see cref="Texture"/> created from <see cref="Stream"/></returns>
        public static Texture LoadTexture(this IResourceLoader source, string name)
        {
            return LoadTexture(source, name,
                              TextureExpansionMode.Bilinear,
                              TextureShrinkMode.Bilinear,
                              TextureMipmapMode.Bilinear,
                              TextureWrapMode.ClampToEdge,
                              TextureWrapMode.ClampToEdge);
        }

        /// <summary>Create <see cref="Texture"/> from resource</summary>
        /// <param name="name">resource name</param>
        /// <param name="expansionMode">texture expansion mode</param>
        /// <param name="shrinkMode">textrue shrink mode</param>
        /// <param name="mipmapMode">texture mipmap mode</param>
        /// <param name="wrapModeX">texture x wrap mode</param>
        /// <param name="wrapModeY">texture y wrap mode</param>
        /// <returns><see cref="Texture"/> create from <see cref="Stream"/></returns>
        public static Texture LoadTexture(this IResourceLoader source, string name, TextureExpansionMode expansionMode,
                                          TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode,
                                          TextureWrapMode wrapModeX, TextureWrapMode wrapModeY)
        {
            var type = Image.GetTypeFromExt(ResourcePath.GetExtension(name));
            using var stream = source.GetStream(name);
            using var image = Image.FromStream(stream, type);
            var texture = new Texture(expansionMode, shrinkMode, mipmapMode, wrapModeX, wrapModeY);
            texture.Load(image);
            return texture;
        }

        public static UniTask<Texture> LoadTextureAsync(this IResourceLoader source, string name,
                                                        AsyncBackEndPoint endPoint,
                                                        FrameLoopTiming timing = FrameLoopTiming.Update,
                                                        CancellationToken cancellationToken = default)
        {
            return LoadTextureAsync(source, name,
                                    TextureExpansionMode.Bilinear,
                                    TextureShrinkMode.Bilinear,
                                    TextureMipmapMode.Bilinear,
                                    TextureWrapMode.ClampToEdge,
                                    TextureWrapMode.ClampToEdge, endPoint, timing, cancellationToken);
        }

        public static async UniTask<Texture> LoadTextureAsync(this IResourceLoader source, string name, TextureExpansionMode expansionMode,
                                                              TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode,
                                                              TextureWrapMode wrapModeX, TextureWrapMode wrapModeY,
                                                              AsyncBackEndPoint endPoint,
                                                              FrameLoopTiming timing = FrameLoopTiming.Update,
                                                              CancellationToken cancellationToken = default)
        {
            if(endPoint is null) {
                throw new ArgumentNullException(nameof(endPoint));
            }
            timing.ThrowArgExceptionIfInvalid(nameof(timing));
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.SwitchToThreadPool();
            // -------------------------------------
            // ↓ thread pool

            cancellationToken.ThrowIfCancellationRequested();

            var type = Image.GetTypeFromExt(ResourcePath.GetExtension(name));
            using var stream = source.GetStream(name);
            using var image = Image.FromStream(stream, type);
            var texture = new Texture(expansionMode, shrinkMode, mipmapMode, wrapModeX, wrapModeY);

            // ↑ thread pool
            // -------------------------------------
            await endPoint.ToTiming(timing, cancellationToken);
            // -------------------------------------
            // ↓ main thread

            unsafe {
                texture.Load(new(image.Width, image.Height), new(&BuildImage), image);
            }
            return texture;

            static void BuildImage(Image original, ImageRef image)
            {
                original.GetPixels().CopyTo(image.GetPixels());
            }
        }

        public static Typeface LoadTypeface(this IResourceLoader source, string name)
        {
            using var stream = source.GetStream(name);
            return new Typeface(stream);
        }
    }
}
