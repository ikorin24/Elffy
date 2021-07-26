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
        public static UniTask<AsyncUnit> Activate(this FrameObject source, FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            return source.Activate(Game.Layers.WorldLayer, timing, cancellationToken);
        }
    }
}
