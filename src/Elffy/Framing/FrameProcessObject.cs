#nullable enable
using System;
using System.Collections.Generic;

namespace Elffy.Framing
{
    /// <summary><see cref="FrameProcess"/> の1つの動作を表すデリゲード</summary>
    /// <param name="process">現在の動作の情報</param>
    public delegate void FrameProcessBehavior(FrameProcessBehaviorInfo process);

    #region internal class FrameProcessObject
    /// <summary>
    /// <see cref="FrameProcess"/> の動作を処理する実体となるオブジェクト<para/>
    /// 一つの <see cref="FrameProcess"/> と1つの <see cref="FrameProcessObject"/> が対応している
    /// </summary>
    internal class FrameProcessObject : FrameObject
    {
        #region private member
        /// <summary>このフレームプロセスの処理のキュー</summary>
        private readonly Queue<BehaviorQueueObject> _behaviorQueue = new Queue<BehaviorQueueObject>();
        /// <summary>このフレームプロセスの現在実行中の処理</summary>
        private FrameProcessBehavior? _currentBehavior;
        /// <summary>現在実行中の処理に渡される情報</summary>
        private FrameProcessBehaviorInfo _info = new FrameProcessBehaviorInfo();
        /// <summary>各処理の開始時刻</summary>
        private TimeSpan _firstFrameTime;
        /// <summary>現在のフレームが処理の最初のフレームかどうか</summary>
        private bool _isFirstFrame;
        /// <summary>このフレームプロセスがキャンセルされたかどうか</summary>
        private bool _isCanceled;
        #endregion private member

        public FrameProcessObject()
        {
            Updated += OnUpdated;
        }

        #region AddBehavior
        /// <summary>キューに処理を追加します</summary>
        /// <param name="time">処理の寿命</param>
        /// <param name="behavior">処理</param>
        public void AddBehavior(TimeSpan time, FrameProcessBehavior behavior)
        {
            var lifeSpan = time;
            _behaviorQueue.Enqueue(new BehaviorQueueObject(behavior, lifeSpan));
            if(_currentBehavior == null) {
                SetNextBehavior();
            }
        }

        /// <summary>継続条件を指定してキューに処理を追加</summary>
        /// <param name="condition">継続条件</param>
        /// <param name="behavior">処理</param>
        public void AddBehavior(Func<bool> condition, FrameProcessBehavior behavior)
        {
            _behaviorQueue.Enqueue(new BehaviorQueueObject(behavior, condition));
            if(_currentBehavior == null) {
                SetNextBehavior();
            }
        }

        /// <summary>寿命1フレームの処理をキューに追加</summary>
        /// <param name="behavior">処理</param>
        public void AddBehavior(FrameProcessBehavior behavior)
        {
            const int frameSpan = 1;     // 寿命は1フレーム
            _behaviorQueue.Enqueue(new BehaviorQueueObject(behavior, frameSpan));
            if(_currentBehavior == null) {
                SetNextBehavior();
            }
        }
        #endregion

        /// <summary>このフレームプロセスをキャンセルします</summary>
        public void Cancel() => _isCanceled = true;

        /// <summary>フレーム毎の更新処理</summary>
        private void OnUpdated(FrameObject sender)
        {
            if(_isFirstFrame) {
                _firstFrameTime = Game.Time;
                _isFirstFrame = false;
            }
            _info.Time = Game.Time - _firstFrameTime;
            // キャンセルされたら終了
            if(_isCanceled) {
                _behaviorQueue.Clear();
                _currentBehavior = null;
                Terminate();
                return;
            }
            // 寿命が終了 or 継続条件がfalse なら次の動きに遷移。次がなければ終了。
            while(true){
                bool end;
                switch(_info.Mode) {
                    case FrameProcessEndMode.LifeSpen:
                        end = _info.Time >= _info.LifeSpan;
                        break;
                    case FrameProcessEndMode.Condition:
                        end = !_info.Condition!();
                        break;
                    case FrameProcessEndMode.FrameSpan:
                        end = _info.FrameNum >= _info.FrameSpan;
                        break;
                    default:
                        end = true;
                        break;
                }
                if(end) {
                    if(_behaviorQueue.Count == 0) {
                        _currentBehavior = null;
                        Terminate();
                        return;
                    }
                    else {
                        SetNextBehavior();
                    }
                }
                else {
                    break;
                }
            }

            _currentBehavior!(_info);     // フレームプロセスの実行
            _info.FrameNum++;
        }

        #region SetNextBehavior
        private void SetNextBehavior()
        {
            var tmp = _behaviorQueue.Dequeue();
            _currentBehavior = tmp.Behavior;
            _info.Condition = tmp.Condition;
            _info.LifeSpan = tmp.LifeSpan;
            _info.FrameSpan = tmp.FrameSpan;
            _info.Mode = tmp.Mode;
            _info.FrameNum = 0;
            _isFirstFrame = true;
        }
        #endregion

        #region strcut BehaviorQueueObject
        /// <summary>フレームプロセスの処理キューに入れるオブジェクト</summary>
        private struct BehaviorQueueObject      // TODO: struct である意味を要再検討。ないなら class に
        {
            /// <summary>処理</summary>
            public FrameProcessBehavior Behavior { get; private set; }
            /// <summary>寿命 (ない場合は0)</summary>
            public TimeSpan LifeSpan { get; private set; }
            /// <summary>寿命フレーム (ない場合0)</summary>
            public int FrameSpan { get; private set; }
            /// <summary>継続条件 (ない場合はnull)</summary>
            public Func<bool>? Condition { get; private set; }
            /// <summary>終了モード</summary>
            public FrameProcessEndMode Mode { get; private set; }

            /// <summary>寿命付き処理のオブジェクトを生成</summary>
            /// <param name="behavior">処理</param>
            /// <param name="lifeSpan">寿命</param>
            public BehaviorQueueObject(FrameProcessBehavior behavior, TimeSpan lifeSpan)
            {
                Behavior = behavior;
                LifeSpan = lifeSpan;
                FrameSpan = 0;
                Mode = FrameProcessEndMode.LifeSpen;
                Condition = null;
            }

            /// <summary>寿命付き処理のオブジェクトを生成</summary>
            /// <param name="behavior">処理</param>
            /// <param name="lifeSpan">寿命</param>
            public BehaviorQueueObject(FrameProcessBehavior behavior, int frameSpan)
            {
                Behavior = behavior;
                LifeSpan = TimeSpan.Zero;
                FrameSpan = frameSpan;
                Mode = FrameProcessEndMode.FrameSpan;
                Condition = null;
            }

            /// <summary>終了条件付き処理のオブジェクトを生成</summary>
            /// <param name="behavior">処理</param>
            /// <param name="condition">終了条件</param>
            public BehaviorQueueObject(FrameProcessBehavior behavior, Func<bool> condition)
            {
                Behavior = behavior;
                LifeSpan = TimeSpan.Zero;
                FrameSpan = 0;
                Mode = FrameProcessEndMode.Condition;
                Condition = condition;
            }
        }
        #endregion struct QueueObject
    }
    #endregion internal class FrameProcessObject

    #region enum FrameProcessEndMode
    public enum FrameProcessEndMode
    {
        /// <summary>寿命時間で終了します</summary>
        LifeSpen,
        /// <summary>終了条件で終了します</summary>
        Condition,
        /// <summary>指定のフレーム回数で終了します</summary>
        FrameSpan,
    }
    #endregion enum FrameProcessEndMode
}
