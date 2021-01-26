#nullable enable
using System;
using System.Threading;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Elffy.Threading;

namespace Elffy
{
    public sealed class FrameEnumerableSource
    {
        private readonly AsyncBackEndPoint _endPoint;
          
        internal FrameEnumerableSource(AsyncBackEndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        public FrameAsyncEnumerable OnTiming(FrameLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return new FrameAsyncEnumerable(_endPoint, timing, cancellationToken);
        }
    }

    public readonly struct FrameAsyncEnumerable : IUniTaskAsyncEnumerable<FrameInfo>
    {
        private readonly AsyncBackEndPoint _endPoint;
        private readonly FrameLoopTiming _timing;
        private readonly CancellationToken _cancellationToken;

        internal FrameAsyncEnumerable(AsyncBackEndPoint endPoint, FrameLoopTiming timing, CancellationToken cancellation)
        {
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
        private readonly IHostScreen _screen;
        private readonly CancellationToken _cancellationToken;
        private readonly TimeSpan _startTime;
        private readonly long _startFrame;
        private readonly FrameLoopTiming _timing;

        public FrameInfo Current => new FrameInfo(_screen.FrameNum - _startFrame, _screen.Time - _startTime);

        internal FrameAsyncEnumerator(IHostScreen screen, FrameLoopTiming timing, CancellationToken cancellationToken)
        {
            _screen = screen;
            _cancellationToken = cancellationToken;
            _startTime = screen.Time + screen.FrameDelta;
            _startFrame = screen.FrameNum + 1;
            _timing = timing;
        }

        public UniTask DisposeAsync() => UniTask.CompletedTask;

        public async UniTask<bool> MoveNextAsync()
        {
            if(_cancellationToken.IsCancellationRequested) {
                return false;
            }
            await _screen.AsyncBack.ToTiming(_timing);
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
