#nullable enable
using System;
using System.Threading;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Elffy.Threading;

namespace Elffy
{
    public readonly struct FrameAsyncEnumerable : IUniTaskAsyncEnumerable<FrameInfo>
    {
        private readonly AsyncBackEndPoint _endPoint;
        private readonly FrameTiming _timing;
        private readonly CancellationToken _cancellationToken;

        internal FrameAsyncEnumerable(AsyncBackEndPoint endPoint, FrameTiming timing, CancellationToken cancellation)
        {
            Debug.Assert(timing.IsSpecified());
            _endPoint = endPoint;
            _timing = timing;
            _cancellationToken = cancellation;
        }

        private FrameAsyncEnumerable(in FrameAsyncEnumerable old, CancellationToken cancellationToken)
        {
            _endPoint = old._endPoint;
            _timing = old._timing;
            _cancellationToken = cancellationToken;
        }

        public FrameAsyncEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new FrameAsyncEnumerator(_endPoint.Screen, _timing, CancellationTokenHelper.Combine(_cancellationToken, cancellationToken));
        }

        IUniTaskAsyncEnumerator<FrameInfo> IUniTaskAsyncEnumerable<FrameInfo>.GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return GetAsyncEnumerator(cancellationToken);
        }

        public FrameAsyncEnumerable WithCancellation(CancellationToken cancellationToken)
        {
            return new FrameAsyncEnumerable(this, CancellationTokenHelper.Combine(_cancellationToken, cancellationToken));
        }
    }

    public readonly struct FrameAsyncEnumerator : IUniTaskAsyncEnumerator<FrameInfo>
    {
        private readonly FrameTimingPoint _timingPoint;
        private readonly CancellationToken _cancellationToken;
        private readonly TimeSpan _startTime;
        private readonly long _startFrame;

        public FrameInfo Current
        {
            get
            {
                var screen = _timingPoint.Screen;
                return new FrameInfo(screen.FrameNum - _startFrame, screen.Time - _startTime);
            }
        }

        internal FrameAsyncEnumerator(IHostScreen screen, FrameTiming timing, CancellationToken cancellationToken)
        {
            Debug.Assert(timing.IsSpecified());
            _timingPoint = screen.AsyncBack.TimingOf(timing);
            _cancellationToken = cancellationToken;
            _startTime = screen.Time + screen.FrameDelta;
            _startFrame = screen.FrameNum + 1;
        }

        public UniTask DisposeAsync() => UniTask.CompletedTask;

        public async UniTask<bool> MoveNextAsync()
        {
            if(_cancellationToken.IsCancellationRequested) {
                return false;
            }
            await _timingPoint.Switch();
            return true;
        }
    }

    [DebuggerDisplay("{DebugDisplay}")]
    public readonly struct FrameInfo : IEquatable<FrameInfo>
    {
        public readonly long FrameNum;
        public readonly TimeSpan Time;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugDisplay => $"{nameof(FrameNum)}: {FrameNum}, {nameof(Time)}: {Time}";

        internal FrameInfo(in long frameNum, in TimeSpan time)
        {
            FrameNum = frameNum;
            Time = time;
        }

        public override bool Equals(object? obj) => obj is FrameInfo info && Equals(info);

        public bool Equals(FrameInfo other) => FrameNum == other.FrameNum && Time.Equals(other.Time);

        public override int GetHashCode() => HashCode.Combine(FrameNum, Time);

        public override string ToString() => DebugDisplay;
    }
}
