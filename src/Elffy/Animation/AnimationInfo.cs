using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Animation
{
    /// <summary><see cref="Animation"/> の現在実行中の処理に渡される情報</summary>
    public struct AnimationInfo
    {
        /// <summary>現在の <see cref="AnimationBehavior"/> の寿命(ms)</summary>
        public int LifeSpan { get; internal set; }

        /// <summary>現在の <see cref="AnimationBehavior"/> が始まってからのフレーム数</summary>
        public int FrameNum { get; internal set; }

        /// <summary>現在の <see cref="AnimationBehavior"/> が始まってからの時間(ms)</summary>
        public int Time { get; internal set; }

        /// <summary><see cref="Mode"/> が <see cref="AnimationEndMode.Condition"/> 現在の場合、この <see cref="AnimationBehavior"/> の終了条件を返します</summary>
        public Func<bool> Condition { get; internal set; }

        /// <summary>この <see cref="AnimationBehavior"/> の終了モード</summary>
        public AnimationEndMode Mode { get; internal set; }
    }
}
