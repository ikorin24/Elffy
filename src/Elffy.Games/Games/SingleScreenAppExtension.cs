#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace Elffy.Games
{
    public static class SingleScreenAppExtension
    {
        public static void Activate(this FrameObject source)
        {
            source.Activate(Game.WorldLayer);
        }
    }
}
