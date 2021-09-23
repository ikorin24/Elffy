#nullable enable
using Elffy.Components;
using Elffy.Imaging;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Elffy
{
    public static class GameResourceLoaderExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Texture> LoadTextureAsync(this IResourceLoader source, string name,
                                                        FrameLoopTiming timing = FrameLoopTiming.Update,
                                                        CancellationToken cancellationToken = default)
        {
            return source.LoadTextureAsync(name, Timing.EndPoint, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Texture> LoadTextureAsync(this IResourceLoader source, string name, TextureConfig config,
                                                        FrameLoopTiming timing = FrameLoopTiming.Update,
                                                        CancellationToken cancellationToken = default)
        {
            return source.LoadTextureAsync(name, config, Timing.EndPoint, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Icon> LoadIconAsync(this IResourceLoader loader,
                                                  string name,
                                                  [AllowNotSpecifiedTiming] FrameLoopTiming timing = FrameLoopTiming.Update,
                                                  CancellationToken cancellationToken = default)
        {
            return loader.LoadIconAsync(name, Timing.EndPoint, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Image> LoadImageAsync(this IResourceLoader loader,
                                                    string name,
                                                    [AllowNotSpecifiedTiming] FrameLoopTiming timing = FrameLoopTiming.Update,
                                                    CancellationToken cancellationToken = default)
        {
            return loader.LoadImageAsync(name, Timing.EndPoint, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Typeface> LoadTypefaceAsync(this IResourceLoader loader,
                                                          string name,
                                                          [AllowNotSpecifiedTiming] FrameLoopTiming timing = FrameLoopTiming.Update,
                                                          CancellationToken cancellationToken = default)
        {
            return loader.LoadTypefaceAsync(name, Timing.EndPoint, timing, cancellationToken);
        }
    }
}
