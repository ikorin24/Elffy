#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Graphics.OpenGL;

namespace Elffy
{
    public abstract class PipelineOperation
    {
        private readonly CancellationTokenSource _runningTokenSource;
        private readonly int _sortNumber;
        private RenderPipeline? _owner;
        private bool _isEnabled;
        private LifeState _state;
        private AsyncEventSource<PipelineOperation>? _activating;
        private AsyncEventSource<PipelineOperation>? _terminating;
        private EventSource<(PipelineOperation, IHostScreen)>? _alive;
        private EventSource<PipelineOperation>? _dead;
        private readonly PipelineOperationTimingPoint _beforeExecute;
        private readonly PipelineOperationTimingPoint _afterExecute;

        public PipelineOperationTimingPoint BeforeExecute => _beforeExecute;
        public PipelineOperationTimingPoint AfterExecute => _afterExecute;

        internal RenderPipeline? Owner => _owner;
        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }
        public int SortNumber => _sortNumber;
        public IHostScreen? Screen => _owner?.Screen;
        public LifeState LifeState => _state;
        public AsyncEvent<PipelineOperation> Activating => new(ref _activating);
        public AsyncEvent<PipelineOperation> Terminating => new(ref _terminating);
        public Event<(PipelineOperation Operation, IHostScreen Screen)> Alive => new(ref _alive);
        public Event<PipelineOperation> Dead => new(ref _dead);
        public CancellationToken RunningToken => _runningTokenSource.Token;

        protected PipelineOperation(int sortNumber)
        {
            _runningTokenSource = new CancellationTokenSource();
            _isEnabled = true;
            _sortNumber = sortNumber;
            _state = LifeState.New;
            _beforeExecute = new PipelineOperationTimingPoint(this);
            _afterExecute = new PipelineOperationTimingPoint(this);
        }

        internal void AbortAllEvents()
        {
            _beforeExecute.AbortAllEvents();
            _afterExecute.AbortAllEvents();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetScreen([MaybeNullWhen(false)] out IHostScreen screen)
        {
            screen = _owner?.Screen;
            return screen is not null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IHostScreen GetValidScreen()
        {
            var screen = _owner?.Screen;
            if(screen is null) {
                ThrowHelper.ThrowInvalidNullScreen();
            }
            return screen;
        }

        internal void OnSizeChangedCallback(IHostScreen screen)
        {
            OnSizeChanged(screen);
        }

        protected abstract void OnSizeChanged(IHostScreen screen);

        internal virtual async UniTask ActivateOnScreen(IHostScreen screen, FrameTimingPoint timingPoint, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(screen);
            var currentContext = Engine.CurrentContext;
            if(currentContext != screen) {
                ContextMismatchException.Throw(currentContext, screen);
            }
            if(_state > LifeState.New) {
                throw new InvalidOperationException($"Cannot activate the pipeline operation twice.");
            }
            ct.ThrowIfCancellationRequested();

            Debug.Assert(_state == LifeState.New);
            Debug.Assert(_owner is null);
            _state = LifeState.Activating;
            _owner = screen.RenderPipeline;
            try {
                await _activating.InvokeIfNotNull(this, ct);
            }
            catch(Exception ex) {
                // If exceptions throw on activating, terminate the layer if possible.
                if(screen.RunningToken.IsCancellationRequested == false) {
                    await timingPoint.NextOrNow(CancellationToken.None);
                    try {
                        await TerminateFromScreen(timingPoint);
                    }
                    catch(Exception ex2) {
                        throw new AggregateException(ex, ex2);
                    }
                }
                throw;  // Throw exceptions of activating.
            }
            finally {
                _activating?.Clear();
            }
            screen.RenderPipeline.Add(this, static x => OnAdded(x));
            await timingPoint.NextFrame(CancellationToken.None);
            Debug.Assert(_state >= LifeState.Alive);
            return;

            static void OnAdded(PipelineOperation self)
            {
                var screen = self.Screen;
                Debug.Assert(self._state == LifeState.Activating);
                Debug.Assert(screen != null);
                self._state = LifeState.Alive;
                try {
                    self._alive?.Invoke((self, screen));
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // ignore exceptions in user code.
                }
                try {
                    self.OnSizeChanged(screen);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // ignore exceptions in user code.
                }
            }
        }

        private protected virtual UniTask BeforeTerminating() => UniTask.CompletedTask;

        internal async UniTask TerminateFromScreen(FrameTimingPoint? timingPoint)
        {
            var context = Engine.CurrentContext;
            var screen = Screen;
            if(context is null) {
                ContextMismatchException.ThrowCurrentContextIsNull();
            }
            if(context != screen) {
                ContextMismatchException.Throw(context, screen);
            }
            if(_state == LifeState.New) {
                throw new InvalidOperationException("Cannot terminate the pipeline operation because it is not activated.");
            }
            if(_state >= LifeState.Terminating) {
                throw new InvalidOperationException("Cannot terminate the pipeline operation twice.");
            }
            Debug.Assert(_state == LifeState.Activating || _state == LifeState.Alive);

            if(_state == LifeState.Alive) {
                _state = LifeState.Terminating;
            }
            _runningTokenSource.Cancel();
            var owner = _owner;
            Debug.Assert(owner != null);
            owner.Remove(this, static x => OnRemoved(x));

            await BeforeTerminating();

            // I don't care about exceptions in terminating event
            // because the layer is already registered to the removed list.
            // That means the layer will be dead in the next frame even if exceptions are thrown.
            await _terminating.InvokeIfNotNull(this, CancellationToken.None);

            await (timingPoint ?? screen.Timings.Update).NextFrame(CancellationToken.None);
            Debug.Assert(_state == LifeState.Dead);
            return;

            static void OnRemoved(PipelineOperation self)
            {
                Debug.Assert(self._state == LifeState.Terminating || self._state == LifeState.Activating);
                self._owner = null;
                self._state = LifeState.Dead;
                self.AbortAllTimingPointEvents();
                try {
                    self._dead?.Invoke(self);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Ignore exceptions in user code.
                }
            }
        }

        protected abstract void OnBeforeExecute(IHostScreen screen, ref FBO currentFbo);
        protected abstract void OnAfterExecute(IHostScreen screen, ref FBO currentFbo);

        protected abstract void OnExecute(IHostScreen screen);

        internal void Execute(IHostScreen screen, ref FBO currentFbo)
        {
            _beforeExecute.DoQueuedEvents();
            OnBeforeExecute(screen, ref currentFbo);
            OnExecute(screen);
            OnAfterExecute(screen, ref currentFbo);
            _afterExecute.DoQueuedEvents();
        }

        private void AbortAllTimingPointEvents()
        {
            _beforeExecute.AbortAllEvents();
            _afterExecute.AbortAllEvents();
        }
    }

    public static class PipelineOperationExtensions
    {
        public static async UniTask<T> Activate<T>(this T operation, IHostScreen screen, CancellationToken cancellationToken = default) where T : PipelineOperation
        {
            await operation.ActivateOnScreen(screen, screen.Timings.Update, cancellationToken);
            Debug.Assert(operation.LifeState >= LifeState.Alive);
            return operation;
        }

        public static async UniTask<T> Activate<T>(this T operation, IHostScreen screen, FrameTimingPoint timingPoint, CancellationToken cancellationToken = default) where T : PipelineOperation
        {
            await operation.ActivateOnScreen(screen, timingPoint, cancellationToken);
            Debug.Assert(operation.LifeState >= LifeState.Alive);
            return operation;
        }

        public static UniTask<T> Terminate<T>(this T operation) where T : PipelineOperation
        {
            return Terminate(operation, FrameTiming.Update);
        }

        public static async UniTask<T> Terminate<T>(this T operation, FrameTimingPoint timingPoint) where T : PipelineOperation
        {
            ArgumentNullException.ThrowIfNull(timingPoint);
            await operation.TerminateFromScreen(timingPoint);
            Debug.Assert(operation.LifeState == LifeState.Dead);
            return operation;
        }

        public static async UniTask<T> Terminate<T>(this T operation, FrameTiming timing) where T : PipelineOperation
        {
            var timingPoint = operation.TryGetScreen(out var screen) ?
                                (screen.Timings.TryGetTiming(timing, out var tp) ? tp : null)
                                : null;
            await operation.TerminateFromScreen(timingPoint);
            Debug.Assert(operation.LifeState == LifeState.Dead);
            return operation;
        }
    }
}
