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
        public static UniTask<Texture> LoadTextureAsync(this ResourceFile file, CancellationToken cancellationToken = default)
        {
            return file.LoadTextureAsync(TextureConfig.Default, Timing.TimingPoints, FrameTiming.Update, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Texture> LoadTextureAsync(this ResourceFile file, FrameTiming timing, CancellationToken cancellationToken = default)
        {
            return file.LoadTextureAsync(TextureConfig.Default, Timing.TimingPoints, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Texture> LoadTextureAsync(this ResourceFile file, in TextureConfig config,
                                                        CancellationToken cancellationToken = default)
        {
            return file.LoadTextureAsync(config, Timing.TimingPoints, FrameTiming.Update, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Texture> LoadTextureAsync(this ResourceFile file, in TextureConfig config, FrameTiming timing,
                                                        CancellationToken cancellationToken = default)
        {
            return file.LoadTextureAsync(config, Timing.TimingPoints, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Image> LoadImageAsync(this ResourceFile file,
                                                    CancellationToken cancellationToken = default)
        {
            return file.LoadImageAsync(Timing.TimingPoints, FrameTiming.Update, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Image> LoadImageAsync(this ResourceFile file,
                                                    [AllowNotSpecifiedTiming] FrameTiming timing,
                                                    CancellationToken cancellationToken = default)
        {
            return file.LoadImageAsync(Timing.TimingPoints, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Typeface> LoadTypefaceAsync(this ResourceFile file, CancellationToken cancellationToken = default)
        {
            return file.LoadTypefaceAsync(Timing.TimingPoints, FrameTiming.Update, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Typeface> LoadTypefaceAsync(this ResourceFile file,
                                                          [AllowNotSpecifiedTiming] FrameTiming timing,
                                                          CancellationToken cancellationToken = default)
        {
            return file.LoadTypefaceAsync(Timing.TimingPoints, timing, cancellationToken);
        }
    }
}
