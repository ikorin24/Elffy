#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Features.Internal;

namespace Elffy
{
    /// <summary>Layer class which has the list of <see cref="FrameObject"/></summary>
    [DebuggerDisplay("{GetType().Name,nq}: {Name} (ObjectCount = {ObjectCount}, IsVisible = {IsVisible})")]
    public abstract class Layer
    {
        private readonly FrameObjectStore _store;
        private readonly LayerTimingPointList _timingPoints;
        private LayerCollection? _owner;
        private readonly int _sortNumber;
        private bool _isVisible;
        private LayerLifeState _state;
        private AsyncEventRaiser<Layer>? _activating;

        internal LayerCollection? Owner => _owner;

        /// <inheritdoc/>
        public bool IsVisible { get => _isVisible; set => _isVisible = value; }

        public int SortNumber => _sortNumber;

        public LayerTimingPointList TimingPoints => _timingPoints;

        public IHostScreen? Screen => _owner?.Screen;

        /// <summary>Get count of alive objects in the current frame.</summary>
        public int ObjectCount => _store.ObjectCount;

        public LayerLifeState LifeState => _state;

        public AsyncEvent<Layer> Activating => new(ref _activating);

        protected ReadOnlySpan<FrameObject> Objects => _store.List;
        protected ReadOnlySpan<FrameObject> AddedObjects => _store.Added;
        protected ReadOnlySpan<FrameObject> RemovedObjects => _store.Removed;
        protected ReadOnlySpan<Renderable> Renderables => _store.Renderables;

        protected Layer(int sortNumber) : this(sortNumber, 32)
        {
        }

        private protected Layer(int sortNumber, int capacity)
        {
            _isVisible = true;
            _sortNumber = sortNumber;
            _timingPoints = new LayerTimingPointList(this);
            _store = FrameObjectStore.New(capacity);
            _state = LayerLifeState.New;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetHostScreen([MaybeNullWhen(false)] out IHostScreen screen)
        {
            screen = _owner?.Screen;
            return screen is not null;
        }

        internal async UniTask<Layer> ActivateOnScreen(IHostScreen screen, FrameTimingPoint timingPoint, CancellationToken cancellationToken)
        {
            if(screen is null) { ThrowNullArg(); }
            if(Engine.CurrentContext != screen) { ThrowContextMismatch(); }
            cancellationToken.ThrowIfCancellationRequested();
            if(_state == LayerLifeState.Activating) { ThrowNowActivating(); }
            if(_state.IsAfter(LayerLifeState.New)) {
                await timingPoint.NextOrNow(cancellationToken);
                return this;
            }

            Debug.Assert(_state == LayerLifeState.New);
            Debug.Assert(_owner is null);
            _state = LayerLifeState.Activating;
            _owner = screen.Layers;
            screen.Layers.Add(this);

            await _activating.RaiseIfNotNull(this, cancellationToken);
            _activating?.Clear();
            await WaitForNextFrame(screen, timingPoint, cancellationToken);

            Debug.Assert(_state.IsSameOrAfter(LayerLifeState.Alive));
            return this;

            [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(screen));
            [DoesNotReturn] static void ThrowContextMismatch() => throw new InvalidOperationException("Invalid current context.");
            [DoesNotReturn] static void ThrowNowActivating() => throw new InvalidOperationException($"Cannot call Activate method when the life state is {LayerLifeState.Activating}.");
        }

        private static async UniTask WaitForNextFrame(IHostScreen screen, FrameTimingPoint timingPoint, CancellationToken cancellationToken)
        {
            await screen.TimingPoints.FrameInitializing.Next(cancellationToken);
            await timingPoint.NextOrNow(cancellationToken);
        }

        internal void OnAddedToListCallback(LayerCollection owner)
        {
            Debug.Assert(_state == LayerLifeState.Activating);
            _state = LayerLifeState.Alive;
            OnAlive(owner.Screen);
        }

        internal void OnLayerTerminatedCallback()
        {
            _owner = null;
            _timingPoints.AbortAllEvents();
            OnLayerTerminated();
        }

        internal void AddFrameObject(FrameObject frameObject)
        {
            Debug.Assert(TryGetHostScreen(out var screen) && screen.CurrentTiming.IsOutOfFrameLoop() == false);
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
        internal void Render(IHostScreen screen)
        {
            RenderOverride(screen);
        }

        private protected virtual void RenderOverride(IHostScreen screen)
        {
            var timingPoints = _timingPoints;
            timingPoints.BeforeRendering.DoQueuedEvents();
            SelectMatrix(screen, out var view, out var projection);
            OnRendering(screen);
            if(_isVisible) {
                _store.Render(view, projection);
            }
            OnRendered(screen);
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

        protected abstract void OnRendering(IHostScreen screen);

        protected abstract void OnRendered(IHostScreen screen);

        protected abstract void OnAlive(IHostScreen screen);

        protected abstract void OnLayerTerminated();

        protected abstract void OnSizeChanged(IHostScreen screen);
    }

    public static class LayerExtension
    {
        public static async UniTask<TLayer> Activate<TLayer>(this TLayer layer, IHostScreen screen, CancellationToken cancellationToken = default) where TLayer : Layer
        {
            await layer.ActivateOnScreen(screen, screen.TimingPoints.Update, cancellationToken);
            Debug.Assert(layer.LifeState.IsSameOrAfter(LayerLifeState.Alive));
            return layer;
        }

        public static async UniTask<TLayer> Activate<TLayer>(this TLayer layer, IHostScreen screen, FrameTimingPoint timingPoint, CancellationToken cancellationToken = default) where TLayer : Layer
        {
            await layer.ActivateOnScreen(screen, timingPoint, cancellationToken);
            Debug.Assert(layer.LifeState.IsSameOrAfter(LayerLifeState.Alive));
            return layer;
        }

        // TODO: Terminate Layer
    }
}
