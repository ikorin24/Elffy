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

        public static UniTask<Icon> LoadIconAsync(this ResourceFile file, FrameTimingPointList timingPoints,
                                                  CancellationToken cancellationToken = default)
        {
            return LoadIconAsync(file, timingPoints, FrameTiming.Update, cancellationToken);
        }

        public static UniTask<Icon> LoadIconAsync(this ResourceFile file,
                                                  FrameTimingPointList timingPoints,
                                                  [AllowNotSpecifiedTiming] FrameTiming timing,
                                                  CancellationToken cancellationToken = default)
        {
            if(timingPoints.TryGetTimingOf(timing, out var timingPoint) == false) {
                timingPoint = null;
            }
            return AsyncLoadCore(static file => LoadIcon(file), file,
                                 static icon => icon.Dispose(),
                                 timingPoint, cancellationToken);
        }

        public static Image LoadImage(this ResourceFile file)
        {
            using var stream = file.GetStream();
            return Image.FromStream(stream, Image.GetTypeFromExt(file.FileExtension));
        }

        public static UniTask<Image> LoadImageAsync(this ResourceFile file,
                                                    FrameTimingPointList timingPoints,
                                                    CancellationToken cancellationToken = default)
        {
            return LoadImageAsync(file, timingPoints, FrameTiming.Update, cancellationToken);
        }

        public static UniTask<Image> LoadImageAsync(this ResourceFile file,
                                                    FrameTimingPointList timingPoints,
                                                    [AllowNotSpecifiedTiming] FrameTiming timing,
                                                    CancellationToken cancellationToken = default)
        {
            if(timingPoints.TryGetTimingOf(timing, out var timingPoint) == false) {
                timingPoint = null;
            }
            return AsyncLoadCore(static file => LoadImage(file), file,
                                 static image => image.Dispose(),
                                 timingPoint, cancellationToken);
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
                                                        FrameTimingPointList timingPoints,
                                                        CancellationToken cancellationToken = default)
        {
            return LoadTextureAsync(file, TextureConfig.Default, timingPoints, FrameTiming.Update, cancellationToken);
        }

        public static UniTask<Texture> LoadTextureAsync(ResourceFile file,
                                                        FrameTimingPointList timingPoints,
                                                        FrameTiming timing,
                                                        CancellationToken cancellationToken = default)
        {
            return LoadTextureAsync(file, TextureConfig.Default, timingPoints, timing, cancellationToken);
        }

        public static UniTask<Texture> LoadTextureAsync(this ResourceFile file, TextureConfig config,
                                                              FrameTimingPointList timingPoints,
                                                              CancellationToken cancellationToken = default)
        {
            return LoadTextureAsync(file, config, timingPoints, FrameTiming.Update, cancellationToken);
        }

        public static async UniTask<Texture> LoadTextureAsync(this ResourceFile file, TextureConfig config,
                                                              FrameTimingPointList timingPoints,
                                                              FrameTiming timing,
                                                              CancellationToken cancellationToken = default)
        {
            using var image = await LoadImageAsync(file, timingPoints, timing, cancellationToken);
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
                                                          FrameTimingPointList timingPoints,
                                                          CancellationToken cancellationToken = default)
        {
            return LoadTypefaceAsync(file, timingPoints, FrameTiming.Update, cancellationToken);
        }

        public static UniTask<Typeface> LoadTypefaceAsync(this ResourceFile file,
                                                          FrameTimingPointList timingPoints,
                                                          [AllowNotSpecifiedTiming] FrameTiming timing,
                                                          CancellationToken cancellationToken = default)
        {
            if(timingPoints.TryGetTimingOf(timing, out var timingPoint) == false) {
                timingPoint = null;
            }
            return AsyncLoadCore(static file => LoadTypeface(file), file,
                                 static typeface => typeface.Dispose(),
                                 timingPoint, cancellationToken);
        }

        private static async UniTask<T> AsyncLoadCore<T, TState>(Func<TState, T> onTreadPool, TState state, Action<T>? onCatch,
                                                                 FrameTimingPoint? timingPoint, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.SwitchToThreadPool();
            cancellationToken.ThrowIfCancellationRequested();

            var result = onTreadPool(state);
            if(timingPoint is not null) {
                try {
                    await timingPoint.Switch(cancellationToken);
                }
                catch {
                    onCatch?.Invoke(result);
                    throw;
                }
            }
            return result;
        }
    }
}
