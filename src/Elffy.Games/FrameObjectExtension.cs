#nullable enable
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
        public static UniTask<bool> Activate(this FrameObject source, FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            return source.Activate(Game.Layers.WorldLayer, timing, cancellationToken);
        }
    }
}
