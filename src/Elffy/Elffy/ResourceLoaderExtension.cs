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
        public static Texture LoadTexture(this IResourceLoader resourceLoader, string name)
        {
            return LoadTexture(resourceLoader, name, TextureConfig.Default);
        }


        /// <summary>Create <see cref="Texture"/> from resource</summary>
        /// <param name="source"></param>
        /// <param name="name"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Texture LoadTexture(this IResourceLoader source, string name, in TextureConfig config)
        {
            var type = Image.GetTypeFromExt(ResourcePath.GetExtension(name));
            using var stream = source.GetStream(name);
            using var image = Image.FromStream(stream, type);
            var texture = new Texture(config);
            texture.Load(image);
            return texture;
        }

        public static UniTask<Texture> LoadTextureAsync(this IResourceLoader source, string name,
                                                        AsyncBackEndPoint endPoint,
                                                        FrameLoopTiming timing = FrameLoopTiming.Update,
                                                        CancellationToken cancellationToken = default)
        {
            return LoadTextureAsync(source, name, TextureConfig.Default, endPoint, timing, cancellationToken);
        }

        public static async UniTask<Texture> LoadTextureAsync(this IResourceLoader source, string name, TextureConfig config,
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
            var texture = new Texture(config);

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
