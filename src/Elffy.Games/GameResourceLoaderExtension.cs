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
            return file.LoadTextureAsync(TextureConfig.Default, Timing.Update, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Texture> LoadTextureAsync(this ResourceFile file, in TextureConfig config,
                                                        CancellationToken cancellationToken = default)
        {
            return file.LoadTextureAsync(config, Timing.Update, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Image> LoadImageAsync(this ResourceFile file, CancellationToken cancellationToken = default)
        {
            return file.LoadImageAsync(Timing.Update, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Typeface> LoadTypefaceAsync(this ResourceFile file, CancellationToken cancellationToken = default)
        {
            return file.LoadTypefaceAsync(Timing.Update, cancellationToken);
        }
    }
}
