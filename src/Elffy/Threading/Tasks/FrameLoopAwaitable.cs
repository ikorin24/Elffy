#nullable enable
using System;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Elffy.Threading.Tasks
{
    public readonly struct FrameLoopAwaitable
    {
        private readonly AsyncBackEndPoint _asyncBack;
        private readonly FrameLoopTiming _eventType;
        private readonly CancellationToken _cancellationToken;

        public FrameLoopAwaitable(AsyncBackEndPoint asyncBack, FrameLoopTiming eventType, CancellationToken cancellationToken)
        {
            _asyncBack = asyncBack!;
            _eventType = eventType;
            _cancellationToken = cancellationToken;
        }

        public Awaiter GetAwaiter()
        {
            return new Awaiter(_asyncBack, _eventType, _cancellationToken);
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            private readonly AsyncBackEndPoint _asyncBack;
            private readonly FrameLoopTiming _eventType;
            private readonly CancellationToken _cancellationToken;

            public bool IsCompleted => false;

            public Awaiter(AsyncBackEndPoint asyncBack, FrameLoopTiming eventType, CancellationToken cancellationToken)
            {
                _asyncBack = asyncBack;
                _eventType = eventType;
                _cancellationToken = cancellationToken;
            }

            public void GetResult()
            {
                _cancellationToken.ThrowIfCancellationRequested();
            }

            public void OnCompleted(Action continuation)
            {
                _asyncBack?.Post(_eventType, continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                _asyncBack?.Post(_eventType, continuation);
            }
        }
    }
}
