#nullable enable
using System;
using System.Collections.Generic;

namespace Elffy.Animations
{
    internal struct AnimationBehavior : IEquatable<AnimationBehavior>
    {
        private readonly AnimationBehaviorDelegate _action;
        private readonly AnimationBehaviorInfo _info;
        private bool _isFirstFrame;
        private TimeSpan _startTime;

        internal AnimationBehavior(AnimationBehaviorDelegate action, AnimationBehaviorInfo info)
        {
            _action = action;
            _info = info;
            _isFirstFrame = true;
            _startTime = default;
        }

        /// <summary>現在のフレームでの更新処理を行います</summary>
        /// <param name="behavior">Update 処理を行う <see cref="AnimationBehavior"/></param>
        /// <param name="currentTimeOfHostScreen">処理を行う <see cref="IHostScreen"/> の現在の <see cref="IHostScreen.Time"/></param>
        /// <returns>更新処理が行われたかどうか</returns>
        internal static bool UpdateFrame(ref AnimationBehavior behavior, TimeSpan currentTimeOfHostScreen)
        {
            if(behavior._isFirstFrame) {
                behavior._startTime = currentTimeOfHostScreen;
                behavior._isFirstFrame = false;
            }
            behavior._info.SetTime(currentTimeOfHostScreen - behavior._startTime);

            if(behavior._info.IsEnd) {
                return false;
            }
            else {
                behavior._action(behavior._info);
                behavior._info.SetFrameNum(behavior._info.FrameNum + 1);
                return true;
            }
        }

        public override bool Equals(object? obj) => obj is AnimationBehavior behavior && Equals(behavior);

        public bool Equals(AnimationBehavior other)
        {
            return EqualityComparer<AnimationBehaviorDelegate>.Default.Equals(_action, other._action) &&
                   _info.Equals(other._info) &&
                   _isFirstFrame == other._isFirstFrame &&
                   _startTime.Equals(other._startTime);
        }

        public override int GetHashCode() => HashCode.Combine(_action, _info, _isFirstFrame, _startTime);
    }


    /// <summary><see cref="Animation"/> の1つの動作を表すデリゲード</summary>
    /// <param name="info">現在の動作の情報</param>
    public delegate void AnimationBehaviorDelegate(AnimationBehaviorInfo info);
}
