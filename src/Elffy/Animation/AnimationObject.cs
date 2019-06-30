using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Animation
{
    public delegate void AnimationBehavior(AnimationInfo frame);

    internal class AnimationObject : FrameObject
    {
        #region private member
        private readonly Queue<BehaviorQueueObject> _animationQueue = new Queue<BehaviorQueueObject>();
        private AnimationBehavior _behavior;
        private AnimationInfo _info = new AnimationInfo();
        private long _firstFrameTime;
        private bool _firstFrame;
        #endregion private member

        public bool IsCanceled { get; set; }

        public void AddBehavior(int time, AnimationBehavior behavior)
        {
            var lifeSpan = time;
            _animationQueue.Enqueue(new BehaviorQueueObject(behavior, lifeSpan));
            if(_behavior == null) {
                SetNextBehavior();
            }
        }

        public void AddBehavior(Func<bool> condition, AnimationBehavior behavior)
        {
            _animationQueue.Enqueue(new BehaviorQueueObject(behavior, condition));
            if(_behavior == null) {
                SetNextBehavior();
            }
        }

        public void AddBehavior(AnimationBehavior behavior)
        {
            _animationQueue.Enqueue(new BehaviorQueueObject(behavior, 1));     // 寿命は1フレーム
            if(_behavior == null) {
                SetNextBehavior();
            }
        }

        public override void Update()
        {
            if(_firstFrame) {
                _firstFrameTime = Game.CurrentFrameTime;
                _firstFrame = false;
            }
            _info.Time = (int)(Game.CurrentFrameTime - _firstFrameTime);
            // キャンセルされたら終了
            if(IsCanceled) {
                _animationQueue.Clear();
                _behavior = null;
                Destroy();
                return;
            }
            // 寿命が終了 or 継続条件がfalse なら次の動きに遷移。次がなければ終了。
              if((_info.Mode == AnimationEndMode.LifeSpen && (Game.CurrentFrameTime - _firstFrameTime) >= _info.LifeSpan) ||
                 (_info.Mode == AnimationEndMode.Condition && !_info.Condition())) {
                if(_animationQueue.Count == 0) {
                    _behavior = null;
                    Destroy();
                    return;
                }
                else {
                    SetNextBehavior();
                }
            }

            _behavior(_info);     // アニメーションの実行
            _info.FrameNum++;
        }

        #region SetNextBehavior
        private void SetNextBehavior()
        {
            var tmp = _animationQueue.Dequeue();
            _behavior = tmp.Behavior;
            _info.Condition = tmp.Condition;
            _info.LifeSpan = tmp.LifeSpan;
            _info.Mode = tmp.Mode;
            _info.FrameNum = 0;
            _firstFrame = true;
        }
        #endregion

        #region strcut BehaviorQueueObject
        private struct BehaviorQueueObject
        {
            public AnimationBehavior Behavior { get; private set; }
            public int LifeSpan { get; private set; }
            public Func<bool> Condition { get; private set; }
            public AnimationEndMode Mode { get; private set; }

            public BehaviorQueueObject(AnimationBehavior behavior, int lifeSpan)
            {
                Behavior = behavior;
                LifeSpan = lifeSpan;
                Mode = AnimationEndMode.LifeSpen;
                Condition = null;
            }

            public BehaviorQueueObject(AnimationBehavior behavior, Func<bool> condition)
            {
                Behavior = behavior;
                LifeSpan = 0;
                Mode = AnimationEndMode.Condition;
                Condition = condition;
            }
        }
        #endregion struct QueueObject
    }

    #region enum AnimationEndMode
    public enum AnimationEndMode
    {
        LifeSpen,
        Condition,
    }
    #endregion enum AnimationEndMode
}
