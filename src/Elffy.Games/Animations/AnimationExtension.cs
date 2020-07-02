#nullable enable
using Elffy.Games;

namespace Elffy.Animations
{
    public static class AnimationExtension
    {
        public static void Play(this Animation animation)
        {
            animation.Play(Game.Screen);
        }
    }
}
