﻿#nullable enable
using System;
using System.Collections.Generic;

namespace Elffy.Framing
{
    /// <summary>
    /// <see cref="FrameStream"/> の動作を処理する実体となるオブジェクト<para/>
    /// 一つの <see cref="FrameStream"/> と1つの <see cref="FrameStreamObject"/> が対応している
    /// </summary>
    internal sealed class FrameStreamObject : FrameObject
    {
        private readonly Queue<FrameBehavior> _queue = new Queue<FrameBehavior>();

        private FrameBehavior? _current;

        /// <summary>このフレームプロセスがキャンセルされたかどうか</summary>
        private bool _isCanceled;

        public FrameStreamObject()
        {
            Updated += OnUpdated;
        }

        /// <summary>キューに処理を追加します</summary>
        /// <param name="time">処理の寿命</param>
        /// <param name="behavior">処理</param>
        public void AddLifeSpanBehavior(TimeSpan time, FrameBehaviorDelegate behavior)
        {
            _queue.Enqueue(new FrameBehavior(behavior, FrameBehaviorInfo.LifeSpanMode(time)));
            _current ??= _queue.Dequeue();
        }

        /// <summary>継続条件を指定してキューに処理を追加</summary>
        /// <param name="condition">継続条件</param>
        /// <param name="behavior">処理</param>
        public void AddConditionalBehavior(Func<bool> condition, FrameBehaviorDelegate behavior)
        {
            _queue.Enqueue(new FrameBehavior(behavior, FrameBehaviorInfo.ConditionalMode(condition)));
            _current ??= _queue.Dequeue();
        }

        /// <summary>寿命1フレームの処理をキューに追加</summary>
        /// <param name="behavior">処理</param>
        public void AddFrameSpanBehavior(int frameSpan, FrameBehaviorDelegate behavior)
        {
            _queue.Enqueue(new FrameBehavior(behavior, FrameBehaviorInfo.FrameSpanMode(frameSpan)));
            _current ??= _queue.Dequeue();
        }

        /// <summary>このフレームプロセスをキャンセルします</summary>
        public void Cancel() => _isCanceled = true;

        /// <summary>フレーム毎の更新処理</summary>
        private void OnUpdated(FrameObject sender)
        {
            if(_isCanceled) {
                _queue.Clear();
                _current = null;
                Terminate();
                return;
            }

            while(!_current!.UpdateFrame()) {
                if(_queue.Count > 0) {
                    _current = _queue.Dequeue();
                }
                else {
                    _current = null;
                    Terminate();
                    return;
                }
            }
        }
    }

    public enum FrameBehaviorEndMode
    {
        /// <summary>寿命時間で終了します</summary>
        LifeSpen,
        /// <summary>終了条件で終了します</summary>
        Conditional,
        /// <summary>指定のフレーム回数で終了します</summary>
        FrameSpan,
    }
}