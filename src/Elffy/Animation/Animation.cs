using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Animation
{
    /// <summary>一連のアニメーションの流れを表すオブジェクト</summary>
    public class Animation
    {
        /// <summary>この <see cref="Animation"/> の実体</summary>
        private AnimationObject _animObj;
        /// <summary>何もしない動作を表す <see cref="AnimationBehavior"/> オブジェクト</summary>
        private static readonly AnimationBehavior WAIT_BEHAVIOR = info => { };
        /// <summary>常にtrueを返す条件</summary>
        private static readonly Func<bool> ALWAYS_TRUE = () => true;

        /// <summary>コンストラクタ</summary>
        private Animation() { }

        /// <summary>新しく一連の流れのアニメーションを生成します。</summary>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public static Animation Create() => new Animation();

        /// <summary>指定した時間だけ、指定した処理を毎フレーム実行します</summary>
        /// <param name="time">実行時間(ms)</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public Animation Begin(int time, AnimationBehavior behavior)
        {
            if(time <= 0) { throw new ArgumentException($"{nameof(time)} must be bigger than 1."); }
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }

            var animObj = GetAnimationObject();
            animObj.AddBehavior(time, behavior);
            return this;
        }

        /// <summary>指定した処理を次のフレームに実行します</summary>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public Animation Do(AnimationBehavior behavior)
        {
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }

            var animObj = GetAnimationObject();
            animObj.AddBehavior(behavior);
            return this;
        }

        /// <summary>指定した時間、アニメーションを停止します</summary>
        /// <param name="time">停止時間(ms)</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public Animation Wait(int time)
        {
            if(time < 0) { throw new ArgumentException($"Time must be bigger than 0."); }

            var animObj = GetAnimationObject();
            animObj.AddBehavior(time, WAIT_BEHAVIOR);
            return this;
        }

        /// <summary>指定した条件を満たす間、指定した処理を毎フレーム実行します</summary>
        /// <param name="condition">処理の継続条件</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public Animation While(Func<bool> condition, AnimationBehavior behavior)
        {
            if(condition == null) { throw new ArgumentNullException(nameof(condition)); }
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            var animObj = GetAnimationObject();
            animObj.AddBehavior(condition, behavior);
            return this;
        }

        /// <summary>指定した処理を毎フレーム実行し続けます</summary>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のアニメーションの流れを表す <see cref="Animation"/> オブジェクト</returns>
        public Animation WhileTrue(AnimationBehavior behavior)
        {
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            var animObj = GetAnimationObject();
            animObj.AddBehavior(ALWAYS_TRUE, behavior);
            return this;
        }

        /// <summary>このアニメーションの以降の動作をすべてキャンセルし停止させます</summary>
        public void Cancel() => _animObj?.Cancel();

        #region private Method
        private AnimationObject GetAnimationObject()
        {
            if(_animObj == null) {
                _animObj = new AnimationObject();
                _animObj.Activate();
            }
            return _animObj;
        }
        #endregion
    }
}
