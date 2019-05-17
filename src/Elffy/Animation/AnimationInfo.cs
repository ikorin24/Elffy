using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Animation
{
    public struct AnimationInfo
    {
        public int LifeSpan { get; internal set; }
        public int FrameNum { get; internal set; }
        public Func<bool> Condition { get; internal set; }
        public AnimationEndMode Mode { get; internal set; }
    }
}
