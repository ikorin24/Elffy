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
        public static UniTask<Texture> LoadTextureAsync(this ResourceFile file, FrameTiming timing = FrameTiming.Update,
                                                        CancellationToken cancellationToken = default)
        {
            return file.LoadTextureAsync(TextureConfig.Default, Timing.EndPoint, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Texture> LoadTextureAsync(this ResourceFile file, in TextureConfig config,
                                                        FrameTiming timing = FrameTiming.Update,
                                                        CancellationToken cancellationToken = default)
        {
            return file.LoadTextureAsync(config, Timing.EndPoint, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Image> LoadImageAsync(this ResourceFile file,
                                                    [AllowNotSpecifiedTiming] FrameTiming timing = FrameTiming.Update,
                                                    CancellationToken cancellationToken = default)
        {
            return file.LoadImageAsync(Timing.EndPoint, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Typeface> LoadTypefaceAsync(this ResourceFile file,
                                                          [AllowNotSpecifiedTiming] FrameTiming timing = FrameTiming.Update,
                                                          CancellationToken cancellationToken = default)
        {
            return file.LoadTypefaceAsync(Timing.EndPoint, timing, cancellationToken);
        }
    }
}
