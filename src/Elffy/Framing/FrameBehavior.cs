#nullable enable
using System;

namespace Elffy.Framing
{
    internal sealed class FrameBehavior
    {
        private readonly FrameBehaviorDelegate _action;
        private readonly FrameBehaviorInfo _info;
        private bool _isFirstFrame;
        private TimeSpan _startTime;

        internal FrameBehavior(FrameBehaviorDelegate action, FrameBehaviorInfo info)
        {
            _action = action;
            _info = info;
        }

        internal bool UpdateFrame()
        {
            var currentTime = CurrentScreen.Time;
            if(_isFirstFrame) {
                _startTime = currentTime;
                _isFirstFrame = false;
            }
            _info.SetTime(currentTime - _startTime);

            if(_info.IsEnd) {
                return false;
            }
            else {
                _action(_info);
                _info.SetFrameNum(_info.FrameNum + 1);
                return true;
            }
        }
    }


    /// <summary><see cref="FrameStream"/> の1つの動作を表すデリゲード</summary>
    /// <param name="info">現在の動作の情報</param>
    public delegate void FrameBehaviorDelegate(FrameBehaviorInfo info);
}
