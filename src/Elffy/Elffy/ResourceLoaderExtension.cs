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
        public static Icon LoadIcon(this ResourceFile file)
        {
            using var stream = file.GetStream();
            return IcoParser.Parse(stream);
        }

        public static UniTask<Icon> LoadIconAsync(this ResourceFile file,
                                                  AsyncBackEndPoint endPoint,
                                                  [AllowNotSpecifiedTiming] FrameTiming timing = FrameTiming.Update,
                                                  CancellationToken cancellationToken = default)
        {
            return AsyncLoadCore(static file => LoadIcon(file), file,
                                 static icon => icon.Dispose(),
                                 endPoint, true, timing, cancellationToken);
        }

        public static Image LoadImage(this ResourceFile file)
        {
            using var stream = file.GetStream();
            return Image.FromStream(stream, Image.GetTypeFromExt(file.FileExtension));
        }

        public static UniTask<Image> LoadImageAsync(this ResourceFile file,
                                                    AsyncBackEndPoint endPoint,
                                                    [AllowNotSpecifiedTiming] FrameTiming timing = FrameTiming.Update,
                                                    CancellationToken cancellationToken = default)
        {
            return AsyncLoadCore(static file => LoadImage(file), file,
                                 static image => image.Dispose(),
                                 endPoint, true, timing, cancellationToken);
        }

        public static Texture LoadTexture(this ResourceFile file)
        {
            return LoadTexture(file, TextureConfig.Default);
        }

        public static Texture LoadTexture(this ResourceFile file, in TextureConfig config)
        {
            using var image = LoadImage(file);
            var texture = new Texture(config);
            texture.Load(image);
            return texture;
        }

        public static UniTask<Texture> LoadTextureAsync(ResourceFile file,
                                                        AsyncBackEndPoint endPoint,
                                                        FrameTiming timing = FrameTiming.Update,
                                                        CancellationToken cancellationToken = default)
        {
            return LoadTextureAsync(file, TextureConfig.Default, endPoint, timing, cancellationToken);
        }

        public static async UniTask<Texture> LoadTextureAsync(this ResourceFile file, TextureConfig config,
                                                              AsyncBackEndPoint endPoint,
                                                              FrameTiming timing = FrameTiming.Update,
                                                              CancellationToken cancellationToken = default)
        {
            using var image = await LoadImageAsync(file, endPoint, timing, cancellationToken);
            var texture = new Texture(config);
            texture.Load(image);
            return texture;
        }

        public static Typeface LoadTypeface(this ResourceFile file)
        {
            using var stream = file.GetStream();
            return new Typeface(stream);
        }

        public static UniTask<Typeface> LoadTypefaceAsync(this ResourceFile file,
                                                          AsyncBackEndPoint endPoint,
                                                          [AllowNotSpecifiedTiming] FrameTiming timing = FrameTiming.Update,
                                                          CancellationToken cancellationToken = default)
        {
            return AsyncLoadCore(static file => LoadTypeface(file), file,
                                 static typeface => typeface.Dispose(),
                                 endPoint, true, timing, cancellationToken);
        }


        private static async UniTask<T> AsyncLoadCore<T, TState>(Func<TState, T> onTreadPool, TState state,
                                                                 Action<T>? onCatch,
                                                                 AsyncBackEndPoint endPoint,
                                                                 bool allowNotSpecifiedTiming,
                                                                 FrameTiming timing,
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
                    await endPoint.TimingOf(timing).Switch(cancellationToken);
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
