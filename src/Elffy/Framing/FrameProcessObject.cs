﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private FrameProcessBehavior _currentBehavior;
        /// <summary>現在実行中の処理に渡される情報</summary>
        private FrameProcessBehaviorInfo _info = new FrameProcessBehaviorInfo();
        /// <summary>各処理の開始時刻</summary>
        private long _firstFrameTime;
        /// <summary>現在のフレームが処理の最初のフレームかどうか</summary>
        private bool _isFirstFrame;
        /// <summary>このフレームプロセスがキャンセルされたかどうか</summary>
        private bool _isCanceled;
        #endregion private member

        #region AddBehavior
        /// <summary>キューに処理を追加します</summary>
        /// <param name="time">処理の寿命(ms)</param>
        /// <param name="behavior">処理</param>
        public void AddBehavior(int time, FrameProcessBehavior behavior)
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
            const int lifespan = 1;     // 寿命は1フレーム
            _behaviorQueue.Enqueue(new BehaviorQueueObject(behavior, lifespan));
            if(_currentBehavior == null) {
                SetNextBehavior();
            }
        }
        #endregion

        #region Update
        /// <summary>フレーム毎の更新処理</summary>
        public override void Update()
        {
            if(_isFirstFrame) {
                _firstFrameTime = Game.CurrentFrameTime;
                _isFirstFrame = false;
            }
            _info.Time = (int)(Game.CurrentFrameTime - _firstFrameTime);
            // キャンセルされたら終了
            if(_isCanceled) {
                _behaviorQueue.Clear();
                _currentBehavior = null;
                Destroy();
                return;
            }
            // 寿命が終了 or 継続条件がfalse なら次の動きに遷移。次がなければ終了。
              if((_info.Mode == FrameProcessEndMode.LifeSpen && (Game.CurrentFrameTime - _firstFrameTime) >= _info.LifeSpan) ||
                 (_info.Mode == FrameProcessEndMode.Condition && !_info.Condition())) {
                if(_behaviorQueue.Count == 0) {
                    _currentBehavior = null;
                    Destroy();
                    return;
                }
                else {
                    SetNextBehavior();
                }
            }

            _currentBehavior(_info);     // フレームプロセスの実行
            _info.FrameNum++;
        }
        #endregion

        /// <summary>このフレームプロセスをキャンセルします</summary>
        public void Cancel() => _isCanceled = true;

        #region SetNextBehavior
        private void SetNextBehavior()
        {
            var tmp = _behaviorQueue.Dequeue();
            _currentBehavior = tmp.Behavior;
            _info.Condition = tmp.Condition;
            _info.LifeSpan = tmp.LifeSpan;
            _info.Mode = tmp.Mode;
            _info.FrameNum = 0;
            _isFirstFrame = true;
        }
        #endregion

        #region strcut BehaviorQueueObject
        /// <summary>フレームプロセスの処理キューに入れるオブジェクト</summary>
        private struct BehaviorQueueObject
        {
            /// <summary>処理</summary>
            public FrameProcessBehavior Behavior { get; private set; }
            /// <summary>寿命 (ない場合は0)</summary>
            public int LifeSpan { get; private set; }
            /// <summary>継続条件 (ない場合はnull)</summary>
            public Func<bool> Condition { get; private set; }
            /// <summary>終了モード</summary>
            public FrameProcessEndMode Mode { get; private set; }

            /// <summary>寿命付き処理のオブジェクトを生成</summary>
            /// <param name="behavior">処理</param>
            /// <param name="lifeSpan">寿命</param>
            public BehaviorQueueObject(FrameProcessBehavior behavior, int lifeSpan)
            {
                Behavior = behavior;
                LifeSpan = lifeSpan;
                Mode = FrameProcessEndMode.LifeSpen;
                Condition = null;
            }

            /// <summary>終了条件付き処理のオブジェクトを生成</summary>
            /// <param name="behavior">処理</param>
            /// <param name="condition">終了条件</param>
            public BehaviorQueueObject(FrameProcessBehavior behavior, Func<bool> condition)
            {
                Behavior = behavior;
                LifeSpan = 0;
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
        LifeSpen,
        Condition,
    }
    #endregion enum FrameProcessEndMode
}