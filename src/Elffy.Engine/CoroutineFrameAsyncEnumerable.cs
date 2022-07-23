#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy
{
    public readonly struct CoroutineFrameAsyncEnumerable : IUniTaskAsyncEnumerable<FrameInfo>
    {
        private readonly CoroutineState _coroutineState;
        private readonly FrameTiming _timing;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal CoroutineFrameAsyncEnumerable(CoroutineState coroutineState, FrameTiming timing)
        {
            Debug.Assert(timing.IsSpecified());
            _coroutineState = coroutineState;
            _timing = timing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetAsyncEnumerator() => new Enumerator(_coroutineState, _timing);

        IUniTaskAsyncEnumerator<FrameInfo> IUniTaskAsyncEnumerable<FrameInfo>.GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            // I ignore the cancellationToken.
            return new Enumerator(_coroutineState, _timing);
        }

        public readonly struct Enumerator : IUniTaskAsyncEnumerator<FrameInfo>
        {
            private readonly FrameObject? _frameObject;
            private readonly FrameTimingPoint _timingPoint;
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(CoroutineState coroutineState, FrameTiming timing)
            {
                Debug.Assert(timing.IsSpecified());
                var screen = coroutineState.Screen;
                _frameObject = coroutineState.FrameObject;
                _startTime = screen.Time + screen.FrameDelta;
                _startFrame = screen.FrameNum + 1;
                _timingPoint = screen.Timings.GetTiming(timing);
            }

            public UniTask DisposeAsync() => UniTask.CompletedTask;

            public async UniTask<bool> MoveNextAsync()
            {
                if(CoroutineState.CoroutineCanRun(_timingPoint.Screen, _frameObject) == false) {
                    return false;
                }
                await _timingPoint.Next();
                return true;
            }
        }
    }
}
