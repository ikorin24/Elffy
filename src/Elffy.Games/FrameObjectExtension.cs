#nullable enable
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Elffy
{
    public static class FrameObjectExtension
    {
        /// <summary>Activate <see cref="FrameObject"/> in world layer.</summary>
        /// <param name="source">source object to activate</param>
        /// <param name="timing"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask<T> Activate<T>(this T source, FrameTiming timing = FrameTiming.Update, CancellationToken cancellationToken = default)
            where T : FrameObject
        {
            await source.Activate(Game.Layers.WorldLayer, timing, cancellationToken);
            return source;
        }
    }
}
