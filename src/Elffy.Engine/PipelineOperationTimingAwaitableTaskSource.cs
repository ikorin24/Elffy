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
    internal sealed class PipelineOperationTimingAwaitableTaskSource : IUniTaskSource<AsyncUnit>, IChainInstancePooled<PipelineOperationTimingAwaitableTaskSource>
    {
        private static Int16TokenFactory _tokenFactory;

        private PipelineOperationTimingAwaitableTaskSource? _next;
        private TimingAwaitableCore<PipelineOperationTimingPoint> _awaitableCore;

        public ref PipelineOperationTimingAwaitableTaskSource? NextPooled => ref _next;

        private PipelineOperationTimingAwaitableTaskSource(PipelineOperationTimingPoint? timingPoint, short token, CancellationToken cancellationToken)
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

        internal static UniTask<AsyncUnit> CreateTask(PipelineOperationTimingPoint timingPoint, CancellationToken cancellationToken)
        {
            var token = _tokenFactory.CreateToken();
            if(ChainInstancePool<PipelineOperationTimingAwaitableTaskSource>.TryGetInstanceFast(out var taskSource)) {
                taskSource._awaitableCore = new(timingPoint, token, cancellationToken);
            }
            else {
                taskSource = new PipelineOperationTimingAwaitableTaskSource(timingPoint, token, cancellationToken);
            }
            return new UniTask<AsyncUnit>(taskSource, token);
        }

        private static void Return(PipelineOperationTimingAwaitableTaskSource source)
        {
            source._awaitableCore = default;
            ChainInstancePool<PipelineOperationTimingAwaitableTaskSource>.ReturnInstanceFast(source);
        }
    }
}
