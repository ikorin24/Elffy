#nullable enable

namespace Elffy.Animations
{
    public enum AnimationBehaviorEndMode
    {
        /// <summary>寿命時間で終了します</summary>
        LifeSpen,
        /// <summary>終了条件で終了します</summary>
        Conditional,
        /// <summary>指定のフレーム回数で終了します</summary>
        FrameSpan,
    }
}
