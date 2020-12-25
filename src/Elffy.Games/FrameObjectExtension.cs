#nullable enable
using System.Threading;
using Elffy.Core;
using Cysharp.Threading.Tasks;

namespace Elffy
{
    public static class FrameObjectExtension
    {
        /// <summary>Activate <see cref="FrameObject"/> in world layer.</summary>
        /// <param name="source">source object to activate</param>
        public static void Activate(this FrameObject source)
        {
            source.Activate(Game.Layers.WorldLayer);
        }

        public static UniTask<bool> ActivateWaitLoaded(this Renderable source, FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            source.Activate(Game.Layers.WorldLayer);
            return source.WaitLoaded(timing, cancellationToken);
        }
    }
}
