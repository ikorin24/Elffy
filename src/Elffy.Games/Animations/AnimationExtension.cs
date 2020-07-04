#nullable enable
using Elffy.Games;

namespace Elffy.Animations
{
    public static class AnimationExtension
    {
        public static Animation Play(this Animation animation)
        {
            return animation.Play(Game.Screen);
        }
    }
}
