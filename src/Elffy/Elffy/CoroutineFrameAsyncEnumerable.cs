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
        private readonly FrameLoopTiming _timing;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal CoroutineFrameAsyncEnumerable(CoroutineState coroutineState, FrameLoopTiming timing)
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
            private readonly IHostScreen _screen;
            private readonly FrameObject? _frameObject;
            private readonly TimeSpan _startTime;
            private readonly long _startFrame;
            private readonly FrameLoopTiming _timing;

            public FrameInfo Current => new FrameInfo(_screen.FrameNum - _startFrame, _screen.Time - _startTime);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(CoroutineState coroutineState, FrameLoopTiming timing)
            {
                Debug.Assert(timing.IsSpecified());
                _screen = coroutineState.Screen;
                _frameObject = coroutineState.FrameObject;
                _startTime = _screen.Time + _screen.FrameDelta;
                _startFrame = _screen.FrameNum + 1;
                _timing = timing;
            }

            public UniTask DisposeAsync() => UniTask.CompletedTask;

            public async UniTask<bool> MoveNextAsync()
            {
                if(CoroutineState.CoroutineCanRun(_screen, _frameObject) == false) {
                    return false;
                }
                await _screen.AsyncBack.ToTiming(_timing);
                return true;
            }
        }
    }
}
