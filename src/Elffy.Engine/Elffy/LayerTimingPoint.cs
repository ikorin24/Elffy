#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Features.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy
{
    public sealed class LayerTimingPoint : ITimingPoint
    {
        private readonly ILayer _layer;
        private readonly AsyncEventQueueCore _eventQueue;

        internal ILayer Layer => _layer;

        internal LayerTimingPoint(ILayer layer)
        {
            _layer = layer;
            _eventQueue = AsyncEventQueueCore.New();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<AsyncUnit> Next(CancellationToken cancellationToken = default)
        {
            return LayerTimingAwaitableTaskSource.CreateTask(this, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Post(Action continuation) => _eventQueue.Post(continuation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Post(Action<object?> continuation, object? state) => _eventQueue.Post(continuation, state);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AbortAllEvents() => _eventQueue.AbortAllEvents();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DoQueuedEvents() => _eventQueue.DoQueuedEvents();
    }
}
