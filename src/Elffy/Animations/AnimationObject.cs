#nullable enable
using Elffy.Core;
using System;
using System.Collections.Generic;

namespace Elffy.Animations
{
    /// <summary>
    /// <see cref="Animation"/> の動作を処理する実体となるオブジェクト<para/>
    /// 一つの <see cref="Animation"/> と1つの <see cref="AnimationObject"/> が対応している
    /// </summary>
    internal sealed class AnimationObject : FrameObject
    {
        private readonly Queue<AnimationBehavior> _queue = new Queue<AnimationBehavior>();

        /// <summary>現在実行中の処理</summary>
        private AnimationBehavior _current;
        /// <summary>現在実行中の処理があるかどうか</summary>
        private bool _hasCurrent;
        /// <summary>キャンセルされたかどうか</summary>
        private bool _isCanceled;

        public AnimationObject()
        {
            Updated += OnUpdated;
        }

        public void Activate(SystemLayer systemLayer)
        {
            base.Activate(systemLayer);     // FrameObject.Activate(ILayer)
        }

        /// <summary>キューに処理を追加します</summary>
        /// <param name="time">処理の寿命</param>
        /// <param name="action">処理</param>
        public void AddLifeSpanBehavior(TimeSpan time, AnimationBehaviorDelegate action)
        {
            _queue.Enqueue(new AnimationBehavior(action, AnimationBehaviorInfo.LifeSpanMode(time)));
        }

        /// <summary>継続条件を指定してキューに処理を追加</summary>
        /// <param name="condition">継続条件</param>
        /// <param name="action">処理</param>
        public void AddConditionalBehavior(Func<bool> condition, AnimationBehaviorDelegate action)
        {
            _queue.Enqueue(new AnimationBehavior(action, AnimationBehaviorInfo.ConditionalMode(condition)));
        }

        /// <summary>寿命1フレームの処理をキューに追加</summary>
        /// <param name="action">処理</param>
        public void AddFrameSpanBehavior(int frameSpan, AnimationBehaviorDelegate action)
        {
            _queue.Enqueue(new AnimationBehavior(action, AnimationBehaviorInfo.FrameSpanMode(frameSpan)));
        }

        /// <summary>終了処理をキューに追加</summary>
        public void AddEndBehavior()
        {
            _queue.Enqueue(new AnimationBehavior(_ => Cancel(), AnimationBehaviorInfo.FrameSpanMode(1)));
        }

        /// <summary>即時キャンセルを発行します</summary>
        public void Cancel() => _isCanceled = true;

        /// <summary>フレーム毎の更新処理</summary>
        private void OnUpdated(FrameObject sender)
        {
            if(_isCanceled) {
                _queue.Clear();
                _current = default;
                _hasCurrent = false;
                Terminate();
                return;
            }

            // キューから取り出した処理が既に終了条件を満たしている場合がある。
            // 更新に成功するかキューがなくなるまで次の処理をキューから取り出し続ける。
            while(true) {
                if(_hasCurrent == false) {
                    if(_queue.Count <= 0) { break; }
                    _current = _queue.Dequeue();
                    _hasCurrent = true;
                }
                var successUpdate = AnimationBehavior.UpdateFrame(ref _current, HostScreen.Time);
                if(successUpdate) {
                    break;
                }
                else {
                    _hasCurrent = false;
                }
            }
        }
    }
}
