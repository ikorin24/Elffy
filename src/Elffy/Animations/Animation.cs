#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Animations
{
    // 一度 Play か Cancel を発行すると、それ以降は追加の処理を発行できません。
    // Play の後に新しく処理を追加したい場合は、現在のアニメーションを Cancel して
    // 新しくアニメーションを作成しなおします。
    // 
    // 新しい AnimationObject が必要になるためガベージが出ますが、
    // 将来的に FrameObject のインスタンスをプールする機能を実装することでコストを回避できます。(未実装)
    // AnimationObject は利用頻度が高く、かつ内部に Queue を持つためサイズも大きくなりやすいため
    // インスタンスのプールが有効に働く。


    // TODO: AnimationObject のインスタンスのプールの実装
    //       単一スレッドに限定できるのでパフォーマンスも出しやすいはず

    /// <summary>一連の処理の流れを表すオブジェクト</summary>
    public readonly struct Animation
    {
        private readonly AnimationObject _obj;

        private Animation(AnimationObject obj)
        {
            _obj = obj;
        }

        /// <summary>新しい <see cref="Animation"/> を作成します</summary>
        /// <returns>新しい <see cref="Animation"/></returns>
        public static Animation Define()
        {
            return new Animation(new AnimationObject());  // ほんとは new せずインスタンスをプールから取ってきたい
        }

        /// <summary>指定した処理を指定の時間だけ実行します</summary>
        /// <param name="time">実行する時間</param>
        /// <param name="action">実行する処理</param>
        /// <returns><see cref="Animation"/> オブジェクト</returns>
        public Animation Do(TimeSpan time, AnimationBehaviorDelegate action)
        {
            if(time < TimeSpan.Zero) { throw new ArgumentException($"{nameof(time)} is negative."); }
            if(action is null) { throw new ArgumentNullException(nameof(action)); }
            var obj = GetNotRunningObject();
            if(obj.IsAlive) { }
            obj.AddLifeSpanBehavior(time, action);
            return this;
        }

        /// <summary>指定した処理を次のフレームに実行します</summary>
        /// <param name="action">実行する処理</param>
        /// <returns><see cref="Animation"/> オブジェクト</returns>
        public Animation Do(AnimationBehaviorDelegate action)
        {
            if(action is null) { throw new ArgumentNullException(nameof(action)); }
            var obj = GetNotRunningObject();
            obj.AddFrameSpanBehavior(1, action);
            return this;
        }

        /// <summary>指定した時間、処理をを停止します</summary>
        /// <param name="time">停止時間</param>
        /// <returns><see cref="Animation"/> オブジェクト</returns>
        public Animation Wait(TimeSpan time)
        {
            if(time < TimeSpan.Zero) { throw new ArgumentOutOfRangeException($"{nameof(time)} is negative."); }
            var obj = GetNotRunningObject();
            obj.AddLifeSpanBehavior(time, _ => { });
            return this;
        }

        /// <summary>指定した条件を満たす間、指定した処理を毎フレーム実行します</summary>
        /// <param name="condition">処理の継続条件</param>
        /// <param name="action">実行する処理</param>
        /// <returns><see cref="Animation"/> オブジェクト</returns>
        public Animation While(Func<bool> condition, AnimationBehaviorDelegate action)
        {
            if(condition is null) { throw new ArgumentNullException(nameof(condition)); }
            if(action is null) { throw new ArgumentNullException(nameof(action)); }
            var obj = GetNotRunningObject();
            obj.AddConditionalBehavior(condition, action);
            return this;
        }

        /// <summary>指定した処理を毎フレーム実行し続けます</summary>
        /// <param name="action">実行する処理</param>
        /// <returns><see cref="Animation"/> オブジェクト</returns>
        public Animation Endless(AnimationBehaviorDelegate action)
        {
            if(action is null) { throw new ArgumentNullException(nameof(action)); }
            var obj = GetNotRunningObject();
            obj.AddConditionalBehavior(() => true, action);
            return this;
        }

        /// <summary>処理を全てキャンセルし停止させます。このメソッドが呼ばれた時点で残りの処理に関わらず終了します</summary>
        public void Cancel()
        {
            var obj = _obj ?? throw new InvalidOperationException("Invalid operation");
            obj.Cancel();
            Unsafe.AsRef(_obj) = null!;
        }

        /// <summary>処理を開始します</summary>
        /// <param name="screen">処理を行う <see cref="IHostScreen"/></param>
        public Animation Play(IHostScreen screen)
        {
            var obj = GetNotRunningObject();
            obj.Activate(screen.Layers.SystemLayer);
            obj.AddEndBehavior();
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AnimationObject GetNotRunningObject()
        {
            if(_obj is null || _obj.IsAlive) { throw new InvalidOperationException("Already playing or invalid operation."); }
            return _obj;
        }
    }
}
