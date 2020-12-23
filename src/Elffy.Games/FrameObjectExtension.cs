#nullable enable
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

        public static UniTask<bool> ActivateWaitLoaded(this FrameObject source)
        {
            source.Activate(Game.Layers.WorldLayer);
            return source.WaitLoadedOrDead();
        }
    }
}
