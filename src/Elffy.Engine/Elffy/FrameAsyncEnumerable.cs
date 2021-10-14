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
        private readonly FrameTimingPoint _timingPoint;
        private readonly CancellationToken _cancellationToken;

        internal FrameAsyncEnumerable(FrameTimingPoint timingPoint, CancellationToken cancellationToken)
        {
            Debug.Assert(timingPoint is not null);
            _timingPoint = timingPoint;
            _cancellationToken = cancellationToken;
        }

        private FrameAsyncEnumerable(in FrameAsyncEnumerable old, CancellationToken cancellationToken)
        {
            _timingPoint = old._timingPoint;
            _cancellationToken = cancellationToken;
        }

        public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new Enumerator(_timingPoint, CancellationTokenHelper.Combine(_cancellationToken, cancellationToken));
        }

        IUniTaskAsyncEnumerator<FrameInfo> IUniTaskAsyncEnumerable<FrameInfo>.GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return GetAsyncEnumerator(cancellationToken);
        }

        public FrameAsyncEnumerable WithCancellation(CancellationToken cancellationToken)
        {
            return new FrameAsyncEnumerable(this, CancellationTokenHelper.Combine(_cancellationToken, cancellationToken));
        }

        public readonly struct Enumerator : IUniTaskAsyncEnumerator<FrameInfo>
        {
            private readonly FrameTimingPoint _timingPoint;
            private readonly CancellationToken _cancellationToken;
            private readonly TimeSpanF _startTime;
            private readonly long _startFrame;

            public FrameInfo Current
            {
                get
                {
                    var screen = _timingPoint.Screen;
                    return new FrameInfo(screen.FrameNum - _startFrame, screen.Time - _startTime);
                }
            }

            internal Enumerator(FrameTimingPoint timingPoint, CancellationToken cancellationToken)
            {
                Debug.Assert(timingPoint is not null);
                _timingPoint = timingPoint;
                _cancellationToken = cancellationToken;
                var screen = timingPoint.Screen;
                _startTime = screen.Time + screen.FrameDelta;
                _startFrame = screen.FrameNum + 1;
            }

            public UniTask DisposeAsync() => UniTask.CompletedTask;

            public async UniTask<bool> MoveNextAsync()
            {
                if(_cancellationToken.IsCancellationRequested) {
                    return false;
                }
                await _timingPoint.Next();
                return true;
            }
        }
    }

    [DebuggerDisplay("{DebugDisplay}")]
    public readonly struct FrameInfo : IEquatable<FrameInfo>
    {
        public readonly long FrameNum;
        public readonly TimeSpanF Time;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugDisplay => $"{nameof(FrameNum)}: {FrameNum}, {nameof(Time)}: {Time}";

        internal FrameInfo(in long frameNum, in TimeSpanF time)
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
