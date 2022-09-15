#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Features.Internal;
using Elffy.Graphics.OpenGL;
using Elffy.Threading;

namespace Elffy
{
    /// <summary>Layer class which has the list of <see cref="FrameObject"/></summary>
    [DebuggerDisplay("{GetType().FullName,nq} (ObjectCount = {ObjectCount}, IsEnabled = {IsEnabled})")]
    [Obsolete("Obsolete", true)]
    public abstract class Layer
    {
        private readonly FrameObjectStore _store;
        private readonly CancellationTokenSource _runningTokenSource;
        private readonly LayerTimingPointList _timingPoints;
        private LayerCollection? _owner;
        private readonly int _sortNumber;
        private bool _isEnabled;
        private LifeState _state;
        private AsyncEventSource<Layer>? _activating;
        private AsyncEventSource<Layer>? _terminating;

        internal LayerCollection? Owner => _owner;

        public CancellationToken RunningToken => _runningTokenSource.Token;

        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }

        public int SortNumber => _sortNumber;

        public LayerTimingPointList Timings => _timingPoints;

        public IHostScreen? Screen => _owner?.Screen;

        /// <summary>Get count of alive objects in the current frame.</summary>
        public int ObjectCount => _store.ObjectCount;

        public LifeState LifeState => _state;

        public AsyncEvent<Layer> Activating => new(ref _activating);
        public AsyncEvent<Layer> Terminating => new(ref _terminating);

        protected ReadOnlySpan<FrameObject> Objects => _store.List;
        protected ReadOnlySpan<FrameObject> AddedObjects => _store.Added;
        protected ReadOnlySpan<FrameObject> RemovedObjects => _store.Removed;
        protected ReadOnlySpan<Positionable> Positionables => _store.Positionables;

        protected Layer(int sortNumber) : this(sortNumber, 32)
        {
        }

        private protected Layer(int sortNumber, int capacity)
        {
            _runningTokenSource = new CancellationTokenSource();
            _isEnabled = true;
            _sortNumber = sortNumber;
            _timingPoints = new LayerTimingPointList(this);
            _store = new FrameObjectStore(capacity);
            _state = LifeState.New;
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

        internal virtual async UniTask ActivateOnScreen(IHostScreen screen, FrameTimingPoint timingPoint, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(screen);
            var currentContext = Engine.CurrentContext;
            if(currentContext != screen) {
                ContextMismatchException.Throw(currentContext, screen);
            }
            if(_state > LifeState.New) {
                throw new InvalidOperationException("Cannot activate the layer twice.");
            }
            ct.ThrowIfCancellationRequested();

            Debug.Assert(_state == LifeState.New);
            Debug.Assert(_owner is null);
            _state = LifeState.Activating;
            _owner = screen.Layers;
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
            screen.Layers.Add(this, OnAddedToList);
            await timingPoint.NextFrame(CancellationToken.None);
            Debug.Assert(_state >= LifeState.Alive);
            return;

            static void OnAddedToList(Layer self)
            {
                var screen = self.Screen;
                Debug.Assert(self._state == LifeState.Activating);
                Debug.Assert(screen != null);
                self._state = LifeState.Alive;
                try {
                    self.OnAlive(screen);
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
                throw new InvalidOperationException("Cannot terminate the layer because it is not activated.");
            }
            if(_state >= LifeState.Terminating) {
                throw new InvalidOperationException("Cannot terminate the layer twice.");
            }
            Debug.Assert(_state == LifeState.Activating || _state == LifeState.Alive);

            if(_state == LifeState.Alive) {
                _state = LifeState.Terminating;
            }
            _runningTokenSource.Cancel();
            var owner = _owner;
            Debug.Assert(owner != null);
            owner.Remove(this, OnRemovedFromList);

            await TerminateAllFrameObjects(this);

            // I don't care about exceptions in terminating event
            // because the layer is already registered to the removed list.
            // That means the layer will be dead in the next frame even if exceptions are thrown.
            await _terminating.InvokeIfNotNull(this, CancellationToken.None);

            await (timingPoint ?? screen.Timings.Update).NextFrame(CancellationToken.None);
            Debug.Assert(_state == LifeState.Dead);
            return;

            static UniTask TerminateAllFrameObjects(Layer self)
            {
                using var tasks = new ParallelOperation();
                foreach(var frameObject in self.Objects) {
                    // non-root Positionable is terminated by its parent.
                    if(frameObject.IsPositionable(out var positionable)) {
                        if(positionable.IsRoot) {
                            tasks.Add(CreateTerminationTask(positionable));
                        }
                    }
                    else {
                        tasks.Add(CreateTerminationTask(frameObject));
                    }
                }
                return tasks.WhenAll();

                static async UniTask CreateTerminationTask(FrameObject frameObject)
                {
                    try {
                        await frameObject.Terminate();
                    }
                    catch {
                        if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                        // Ignore exceptions.
                    }
                }
            }

            static void OnRemovedFromList(Layer self)
            {
                Debug.Assert(self._state == LifeState.Terminating || self._state == LifeState.Activating);
                self._owner = null;
                self._state = LifeState.Dead;
                self._timingPoints.AbortAllEvents();
                try {
                    self.OnDead();
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Ignore exceptions in user code.
                }
            }
        }

        internal void AddFrameObject(FrameObject frameObject)
        {
            Debug.Assert(TryGetScreen(out var screen) && screen.CurrentTiming.IsOutOfFrameLoop() == false);
            _store.AddFrameObject(frameObject);
        }

        internal void RemoveFrameObject(FrameObject frameObject)
        {
            _store.RemoveFrameObject(frameObject);
        }

        internal void ApplyAdd() => _store.ApplyAdd();
        internal void ApplyRemove() => _store.ApplyRemove();
        internal void EarlyUpdate() => _store.EarlyUpdate();
        internal void Update() => _store.Update();
        internal void LateUpdate() => _store.LateUpdate();
        internal void ClearFrameObject() => _store.ClearFrameObject();
        internal void Render(IHostScreen screen, ref FBO currentFbo)
        {
            RenderOverride(screen, ref currentFbo);
        }

        internal virtual void RenderShadowMap(IHostScreen screen, in Matrix4 lightViewProjection)
        {
            if(_isEnabled) {
                _store.RenderShadowMap(lightViewProjection);
            }
        }

        private protected virtual void RenderOverride(IHostScreen screen, ref FBO currentFbo)
        {
            var timingPoints = _timingPoints;
            timingPoints.BeforeRendering.DoQueuedEvents();
            OnRendering(screen, ref currentFbo);
            if(_isEnabled) {
                SelectMatrix(screen, out var view, out var projection);
                _store.Render(view, projection);
            }
            OnRendered(screen, ref currentFbo);
            timingPoints.AfterRendering.DoQueuedEvents();
        }

        internal void OnSizeChangedCallback(IHostScreen screen)
        {
            OnSizeChanged(screen);
        }

        protected virtual void SelectMatrix(IHostScreen screen, out Matrix4 view, out Matrix4 projection)
        {
            var camera = screen.Camera;
            view = camera.View;
            projection = camera.Projection;
        }

        protected abstract void OnRendering(IHostScreen screen, ref FBO currentFbo);

        protected abstract void OnRendered(IHostScreen screen, ref FBO currentFbo);

        protected abstract void OnAlive(IHostScreen screen);

        protected abstract void OnDead();

        protected abstract void OnSizeChanged(IHostScreen screen);
    }

    [Obsolete("", true)]
    public static class LayerExtension
    {
        public static async UniTask<TLayer> Activate<TLayer>(this TLayer layer, IHostScreen screen, CancellationToken cancellationToken = default) where TLayer : Layer
        {
            await layer.ActivateOnScreen(screen, screen.Timings.Update, cancellationToken);
            Debug.Assert(layer.LifeState >= LifeState.Alive);
            return layer;
        }

        public static async UniTask<TLayer> Activate<TLayer>(this TLayer layer, IHostScreen screen, FrameTimingPoint timingPoint, CancellationToken cancellationToken = default) where TLayer : Layer
        {
            await layer.ActivateOnScreen(screen, timingPoint, cancellationToken);
            Debug.Assert(layer.LifeState >= LifeState.Alive);
            return layer;
        }

        public static UniTask<TLayer> Terminate<TLayer>(this TLayer layer) where TLayer : Layer
        {
            return Terminate(layer, FrameTiming.Update);
        }

        public static async UniTask<TLayer> Terminate<TLayer>(this TLayer layer, FrameTimingPoint timingPoint) where TLayer : Layer
        {
            ArgumentNullException.ThrowIfNull(timingPoint);
            await layer.TerminateFromScreen(timingPoint);
            Debug.Assert(layer.LifeState == LifeState.Dead);
            return layer;
        }

        public static async UniTask<TLayer> Terminate<TLayer>(this TLayer layer, FrameTiming timing) where TLayer : Layer
        {
            var timingPoint = layer.TryGetScreen(out var screen) ?
                                (screen.Timings.TryGetTiming(timing, out var tp) ? tp : null)
                                : null;
            await layer.TerminateFromScreen(timingPoint);
            Debug.Assert(layer.LifeState == LifeState.Dead);
            return layer;
        }
    }
}
