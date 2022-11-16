#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Features.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy
{
    public sealed class PipelineOperationTimingPoint : ITimingPoint
    {
        private readonly PipelineOperation _operation;
        private EventSource<PipelineOperationTimingPoint>? _event;
        private AsyncEventQueueCore _eventQueue;

        internal PipelineOperation Operation => _operation;

        internal PipelineOperationTimingPoint(PipelineOperation operation)
        {
            _operation = operation;
            _eventQueue = new AsyncEventQueueCore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Event<PipelineOperationTimingPoint> AsEvent() => new(ref _event);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<AsyncUnit> Next(CancellationToken cancellationToken = default)
        {
            return PipelineOperationTimingAwaitableTaskSource.CreateTask(this, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventSubscription<PipelineOperationTimingPoint> Subscribe(Action<PipelineOperationTimingPoint> action)
        {
            return AsEvent().Subscribe(action);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Post(Action continuation) => _eventQueue.Post(continuation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Post(Action<object?> continuation, object? state) => _eventQueue.Post(continuation, state);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AbortAllEvents()
        {
            _event?.Clear();
            _eventQueue.AbortAllEvents();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DoQueuedEvents()
        {
            _event?.Invoke(this);
            _eventQueue.DoQueuedEvents();
        }
    }
}
