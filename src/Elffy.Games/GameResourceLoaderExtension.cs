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
        public static UniTask<Texture> LoadTextureAsync(this ResourceFile file, FrameLoopTiming timing = FrameLoopTiming.Update,
                                                        CancellationToken cancellationToken = default)
        {
            return file.LoadTextureAsync(TextureConfig.Default, Timing.EndPoint, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Texture> LoadTextureAsync(this ResourceFile file, in TextureConfig config,
                                                        FrameLoopTiming timing = FrameLoopTiming.Update,
                                                        CancellationToken cancellationToken = default)
        {
            return file.LoadTextureAsync(config, Timing.EndPoint, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Image> LoadImageAsync(this ResourceFile file,
                                                    [AllowNotSpecifiedTiming] FrameLoopTiming timing = FrameLoopTiming.Update,
                                                    CancellationToken cancellationToken = default)
        {
            return file.LoadImageAsync(Timing.EndPoint, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Typeface> LoadTypefaceAsync(this ResourceFile file,
                                                          [AllowNotSpecifiedTiming] FrameLoopTiming timing = FrameLoopTiming.Update,
                                                          CancellationToken cancellationToken = default)
        {
            return file.LoadTypefaceAsync(Timing.EndPoint, timing, cancellationToken);
        }
    }
}
