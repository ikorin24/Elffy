#nullable enable
using Elffy.Components;
using Elffy.Imaging;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Elffy
{
    public static class ResourceLoaderExtension
    {
        public static Icon LoadIcon(this IResourceLoader resourceLoader, string name)
        {
            using var stream = resourceLoader.GetStream(name);
            return IcoParser.Parse(stream);
        }

        public static UniTask<Icon> LoadIconAsync(this IResourceLoader resourceLoader,
                                                  string name,
                                                  AsyncBackEndPoint endPoint,
                                                  [AllowNotSpecifiedTiming] FrameLoopTiming timing = FrameLoopTiming.Update,
                                                  CancellationToken cancellationToken = default)
        {
            var state = (Loader: resourceLoader, Name: name);
            return AsyncLoadCore(static state => LoadIcon(state.Loader, state.Name), state,
                                 static icon => icon.Dispose(),
                                 endPoint, true, timing, cancellationToken);
        }

        public static Image LoadImage(this IResourceLoader resourceLoader, string name)
        {
            using var stream = resourceLoader.GetStream(name);
            return Image.FromStream(stream, Image.GetTypeFromExt(ResourcePath.GetExtension(name)));
        }

        public static UniTask<Image> LoadImageAsync(this IResourceLoader resourceLoader, string name,
                                                    AsyncBackEndPoint endPoint,
                                                    [AllowNotSpecifiedTiming] FrameLoopTiming timing = FrameLoopTiming.Update,
                                                    CancellationToken cancellationToken = default)
        {
            var state = (Loader: resourceLoader, Name: name);
            return AsyncLoadCore(static state => LoadImage(state.Loader, state.Name), state,
                                 static image => image.Dispose(),
                                 endPoint, true, timing, cancellationToken);
        }

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
            using var image = LoadImage(source, name);
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
            using var image = await LoadImageAsync(source, name, endPoint, timing, cancellationToken);
            var texture = new Texture(config);
            texture.Load(image);
            return texture;
        }

        public static Typeface LoadTypeface(this IResourceLoader source, string name)
        {
            using var stream = source.GetStream(name);
            return new Typeface(stream);
        }

        public static UniTask<Typeface> LoadTypefaceAsync(this IResourceLoader source, string name,
                                                          AsyncBackEndPoint endPoint,
                                                          [AllowNotSpecifiedTiming] FrameLoopTiming timing = FrameLoopTiming.Update,
                                                          CancellationToken cancellationToken = default)
        {
            var state = (Loader: source, Name: name);
            return AsyncLoadCore(static state => LoadTypeface(state.Loader, state.Name), state,
                                 static typeface => typeface.Dispose(),
                                 endPoint, true, timing, cancellationToken);
        }


        private static async UniTask<T> AsyncLoadCore<T, TState>(Func<TState, T> onTreadPool, TState state,
                                                                 Action<T>? onCatch,
                                                                 AsyncBackEndPoint endPoint,
                                                                 bool allowNotSpecifiedTiming,
                                                                 FrameLoopTiming timing,
                                                                 CancellationToken cancellationToken)
        {
            if(endPoint is null) {
                throw new ArgumentNullException(nameof(endPoint));
            }
            if(allowNotSpecifiedTiming) {
                timing.ThrowArgExceptionIfInvalid();
            }
            else {
                timing.ThrowArgExceptionIfNotSpecified();
            }
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.SwitchToThreadPool();
            cancellationToken.ThrowIfCancellationRequested();

            var obj = onTreadPool(state);
            if(timing.IsSpecified()) {
                try {
                    await endPoint.ToTiming(timing, cancellationToken);
                }
                catch {
                    onCatch?.Invoke(obj);
                    throw;
                }
            }
            return obj;
        }
    }
}
