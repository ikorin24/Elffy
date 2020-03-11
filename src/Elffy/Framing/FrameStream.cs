#nullable enable
using System;

namespace Elffy.Framing
{
    internal sealed class FrameStream
    {
        private readonly FrameProcessBehavior _behavior;
        private readonly FrameProcessInfo _info;
        private bool _isFirstFrame;
        private TimeSpan _startTime;

        internal FrameStream(FrameProcessBehavior behavior, FrameProcessInfo info)
        {
            _behavior = behavior;
            _info = info;
        }

        internal bool UpdateFrame()
        {
            var currentTime = Engine.CurrentScreen.Time;
            if(_isFirstFrame) {
                _startTime = currentTime;
                _isFirstFrame = false;
            }
            _info.SetTime(currentTime - _startTime);

            if(_info.IsEnd) {
                return false;
            }
            else {
                _behavior(_info);
                _info.SetFrameNum(_info.FrameNum + 1);
                return true;
            }
        }
    }


    /// <summary><see cref="FrameProcess"/> の1つの動作を表すデリゲード</summary>
    /// <param name="process">現在の動作の情報</param>
    public delegate void FrameProcessBehavior(FrameProcessInfo process);
}
