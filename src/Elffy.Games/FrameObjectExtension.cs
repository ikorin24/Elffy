#nullable enable
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Elffy
{
    public static class FrameObjectExtension
    {
        /// <summary>Activate <see cref="FrameObject"/> in default layer.</summary>
        /// <param name="source">source object to activate</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<T> Activate<T>(this T source, CancellationToken cancellationToken = default)
            where T : FrameObject
        {
            return source.Activate(Game.Layers.DefaultLayer, FrameTiming.Update, cancellationToken);
        }

        /// <summary>Activate <see cref="FrameObject"/> in default layer.</summary>
        /// <param name="source">source object to activate</param>
        /// <param name="timing"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<T> Activate<T>(this T source, FrameTiming timing, CancellationToken cancellationToken = default) where T : FrameObject
        {
            return source.Activate(Game.Layers.DefaultLayer, timing, cancellationToken);
        }
    }
}
