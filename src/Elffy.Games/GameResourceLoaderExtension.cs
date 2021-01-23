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
        public static UniTask<Texture> LoadTextureAsync(this IResourceLoader source, string name, BitmapType bitmapType,
                                                        FrameLoopTiming timing,
                                                        CancellationToken cancellationToken = default)
        {
            return source.LoadTextureAsync(name, bitmapType, Timing.EndPoint, timing, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<Texture> LoadTextureAsync(this IResourceLoader source, string name, BitmapType bitmapType, TextureExpansionMode expansionMode,
                                                              TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode,
                                                              TextureWrapMode wrapModeX, TextureWrapMode wrapModeY,
                                                              FrameLoopTiming timing,
                                                              CancellationToken cancellationToken = default)
        {
            return source.LoadTextureAsync(name, bitmapType, expansionMode, shrinkMode, mipmapMode, wrapModeX, wrapModeY, Timing.EndPoint, timing, cancellationToken);
        }
    }
}
