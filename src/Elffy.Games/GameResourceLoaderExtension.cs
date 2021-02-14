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
        public static UniTask<Texture> LoadTextureAsync(this IResourceLoader source, string name, TextureExpansionMode expansionMode,
                                                              TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode,
                                                              TextureWrapMode wrapModeX, TextureWrapMode wrapModeY,
                                                              FrameLoopTiming timing = FrameLoopTiming.Update,
                                                              CancellationToken cancellationToken = default)
        {
            return source.LoadTextureAsync(name, expansionMode, shrinkMode, mipmapMode, wrapModeX, wrapModeY, Timing.EndPoint, timing, cancellationToken);
        }
    }
}
