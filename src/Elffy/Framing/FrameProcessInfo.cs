#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Framing
{
    /// <summary><see cref="FrameProcess"/> の現在実行中の処理に渡される情報</summary>
    public struct FrameProcessBehaviorInfo
    {
        /// <summary>現在の <see cref="FrameProcessBehavior"/> の寿命</summary>
        public TimeSpan LifeSpan { get; internal set; }

        /// <summary>現在の <see cref="FrameProcessBehavior"/> の寿命フレーム</summary>
        public int FrameSpan { get; internal set; }

        /// <summary>現在の <see cref="FrameProcessBehavior"/> が始まってからのフレーム数</summary>
        public int FrameNum { get; internal set; }

        /// <summary>現在の <see cref="FrameProcessBehavior"/> が始まってからの時間</summary>
        public TimeSpan Time { get; internal set; }

        /// <summary><see cref="Mode"/> が <see cref="FrameProcessEndMode.Condition"/> 現在の場合、この <see cref="FrameProcessBehavior"/> の終了条件を返します</summary>
        public Func<bool>? Condition { get; internal set; }

        /// <summary>この <see cref="FrameProcessBehavior"/> の終了モード</summary>
        public FrameProcessEndMode Mode { get; internal set; }
    }
}
