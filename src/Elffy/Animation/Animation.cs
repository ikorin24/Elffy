using System;

namespace Elffy.Animation
{
    #region class Animation
    /// <summary>一連のアニメーションの流れを表すオブジェクト</summary>
    public sealed class Animation
    {
        /// <summary>何もしない動作を表す <see cref="AnimationBehavior"/> オブジェクト</summary>
        internal static readonly AnimationBehavior WAIT_BEHAVIOR = info => { };
        /// <summary>常にtrueを返す条件</summary>
        internal static readonly Func<bool> ALWAYS_TRUE = () => true;

        /// <summary>この <see cref="Animation"/> の実体</summary>
        internal AnimationObject AnimObj { get; } = new AnimationObject();

        /// <summary>コンストラクタ</summary>
        private Animation() { }

        #region public Method
        /// <summary>指定した時間だけ、指定した処理を毎フレーム実行します</summary>
        /// <param name="time">実行時間(ms)</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public static Animation Begin(int time, AnimationBehavior behavior)
        {
            if(time <= 0) { throw new ArgumentException($"{nameof(time)} must be bigger than 1."); }
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            var animation = new Animation();
            animation.AnimObj.Activate();
            animation.AnimObj.AddBehavior(time, behavior);
            return animation;
        }

        /// <summary>指定した処理を次のフレームに実行します</summary>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public static Animation Do(AnimationBehavior behavior)
        {
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            var animation = new Animation();
            animation.AnimObj.Activate();
            animation.AnimObj.AddBehavior(behavior);
            return animation;
        }

        /// <summary>指定した時間、アニメーションを停止します</summary>
        /// <param name="time">停止時間(ms)</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public static Animation Wait(int time)
        {
            if(time < 0) { throw new ArgumentException($"Time must be bigger than 0."); }
            var animation = new Animation();
            animation.AnimObj.Activate();
            animation.AnimObj.AddBehavior(time, WAIT_BEHAVIOR);
            return animation;
        }

        /// <summary>指定した条件を満たす間、指定した処理を毎フレーム実行します</summary>
        /// <param name="condition">処理の継続条件</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public static Animation While(Func<bool> condition, AnimationBehavior behavior)
        {
            if(condition == null) { throw new ArgumentNullException(nameof(condition)); }
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            var animation = new Animation();
            animation.AnimObj.Activate();
            animation.AnimObj.AddBehavior(condition, behavior);
            return animation;
        }

        /// <summary>指定した処理を毎フレーム実行し続けます</summary>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public static Animation WhileTrue(AnimationBehavior behavior)
        {
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            var animation = new Animation();
            animation.AnimObj.Activate();
            animation.AnimObj.AddBehavior(ALWAYS_TRUE, behavior);
            return animation;
        }
        #endregion public Method
    }
    #endregion

    #region class AnimatinExtension
    public static class AnimatinExtension
    {
        /// <summary>指定した時間だけ、指定した処理を毎フレーム実行します</summary>
        /// <param name="animation">動作を追加するアニメーション</param>
        /// <param name="time">実行時間(ms)</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public static Animation Begin(this Animation animation, int time, AnimationBehavior behavior)
        {
            if(time <= 0) { throw new ArgumentException($"{nameof(time)} must be bigger than 1."); }
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            if(!animation.AnimObj.IsActivated) { animation.AnimObj.Activate(); }
            animation.AnimObj.AddBehavior(time, behavior);
            return animation;
        }

        /// <summary>指定した処理を次のフレームに実行します</summary>
        /// <param name="animation">動作を追加するアニメーション</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public static Animation Do(this Animation animation, AnimationBehavior behavior)
        {
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            if(!animation.AnimObj.IsActivated) { animation.AnimObj.Activate(); }
            animation.AnimObj.AddBehavior(behavior);
            return animation;
        }

        /// <summary>指定した時間、アニメーションを停止します</summary>
        /// <param name="animation">動作を追加するアニメーション</param>
        /// <param name="time">停止時間(ms)</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public static Animation Wait(this Animation animation, int time)
        {
            if(time < 0) { throw new ArgumentException($"Time must be bigger than 0."); }
            if(!animation.AnimObj.IsActivated) { animation.AnimObj.Activate(); }
            animation.AnimObj.AddBehavior(time, Animation.WAIT_BEHAVIOR);
            return animation;
        }

        /// <summary>指定した条件を満たす間、指定した処理を毎フレーム実行します</summary>
        /// <param name="animation">動作を追加するアニメーション</param>
        /// <param name="condition">処理の継続条件</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public static Animation While(this Animation animation, Func<bool> condition, AnimationBehavior behavior)
        {
            if(condition == null) { throw new ArgumentNullException(nameof(condition)); }
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            if(!animation.AnimObj.IsActivated) { animation.AnimObj.Activate(); }
            animation.AnimObj.AddBehavior(condition, behavior);
            return animation;
        }

        /// <summary>指定した処理を毎フレーム実行し続けます</summary>
        /// <param name="animation">動作を追加するアニメーション</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public static Animation WhileTrue(this Animation animation, AnimationBehavior behavior)
        {
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            if(!animation.AnimObj.IsActivated) { animation.AnimObj.Activate(); }
            animation.AnimObj.AddBehavior(Animation.ALWAYS_TRUE, behavior);
            return animation;
        }

        /// <summary>このアニメーションの以降の動作をすべてキャンセルし停止させます</summary>
        public static void Cancel(this Animation animation) => animation.AnimObj.Cancel();
    }
    #endregion
}
