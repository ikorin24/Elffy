#nullable enable
using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Elffy.Features.Internal;
using Elffy.Effective;

namespace Elffy
{
    [Obsolete("", true)]
    internal sealed class LayerTimingAwaitableTaskSource : IUniTaskSource<AsyncUnit>, IChainInstancePooled<LayerTimingAwaitableTaskSource>
    {
        private static Int16TokenFactory _tokenFactory;

        private LayerTimingAwaitableTaskSource? _next;
        private TimingAwaitableCore<LayerTimingPoint> _awaitableCore;

        public ref LayerTimingAwaitableTaskSource? NextPooled => ref _next;

        private LayerTimingAwaitableTaskSource(LayerTimingPoint? timingPoint, short token, CancellationToken cancellationToken)
        {
            _awaitableCore = new(timingPoint, token, cancellationToken);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncUnit GetResult(short token)
        {
            var result = _awaitableCore.GetResult(token);
            Return(this);
            return result;
        }

        [DebuggerHidden]
        void IUniTaskSource.GetResult(short token) => GetResult(token);

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTaskStatus GetStatus(short token) => _awaitableCore.GetStatus(token);

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object?> continuation, object? state, short token) => _awaitableCore.OnCompleted(continuation, state, token);

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTaskStatus UnsafeGetStatus() => _awaitableCore.UnsafeGetStatus();

        internal static UniTask<AsyncUnit> CreateTask(LayerTimingPoint timingPoint, CancellationToken cancellationToken)
        {
            var token = _tokenFactory.CreateToken();
            if(ChainInstancePool<LayerTimingAwaitableTaskSource>.TryGetInstanceFast(out var taskSource)) {
                taskSource._awaitableCore = new(timingPoint, token, cancellationToken);
            }
            else {
                taskSource = new LayerTimingAwaitableTaskSource(timingPoint, token, cancellationToken);
            }
            return new UniTask<AsyncUnit>(taskSource, token);
        }

        private static void Return(LayerTimingAwaitableTaskSource source)
        {
            source._awaitableCore = default;
            ChainInstancePool<LayerTimingAwaitableTaskSource>.ReturnInstanceFast(source);
        }
    }
}
