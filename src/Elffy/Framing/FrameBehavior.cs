#nullable enable
using System;
using System.Collections.Generic;

namespace Elffy.Framing
{
    internal struct FrameBehavior : IEquatable<FrameBehavior>
    {
        private readonly FrameBehaviorDelegate _action;
        private readonly FrameBehaviorInfo _info;
        private bool _isFirstFrame;
        private TimeSpan _startTime;

        internal FrameBehavior(FrameBehaviorDelegate action, FrameBehaviorInfo info)
        {
            _action = action;
            _info = info;
            _isFirstFrame = true;
            _startTime = default;
        }

        internal static bool UpdateFrame(ref FrameBehavior behavior, TimeSpan currentTime)
        {
            if(behavior._isFirstFrame) {
                behavior._startTime = currentTime;
                behavior._isFirstFrame = false;
            }
            behavior._info.SetTime(currentTime - behavior._startTime);

            if(behavior._info.IsEnd) {
                return false;
            }
            else {
                behavior._action(behavior._info);
                behavior._info.SetFrameNum(behavior._info.FrameNum + 1);
                return true;
            }
        }

        public override bool Equals(object? obj) => obj is FrameBehavior behavior && Equals(behavior);

        public bool Equals(FrameBehavior other)
        {
            return EqualityComparer<FrameBehaviorDelegate>.Default.Equals(_action, other._action) &&
                   _info.Equals(other._info) &&
                   _isFirstFrame == other._isFirstFrame &&
                   _startTime.Equals(other._startTime);
        }

        public override int GetHashCode() => HashCode.Combine(_action, _info, _isFirstFrame, _startTime);
    }


    /// <summary><see cref="FrameStream"/> の1つの動作を表すデリゲード</summary>
    /// <param name="info">現在の動作の情報</param>
    public delegate void FrameBehaviorDelegate(FrameBehaviorInfo info);
}
