#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Elffy.Features.Internal;

namespace Elffy
{
    /// <summary>Layer class which has the list of <see cref="FrameObject"/></summary>
    [DebuggerDisplay("{GetType().Name,nq}: {Name} (ObjectCount = {ObjectCount}, IsVisible = {IsVisible})")]
    public class Layer
    {
        private readonly FrameObjectStore _store;
        private readonly LayerTimingPointList _timingPoints;
        private readonly string _name;
        private LayerCollection? _owner;
        private int _sortNumber;
        private bool _isVisible;

        /// <summary>The owner of the layer</summary>
        internal LayerCollection? Owner => _owner;

        /// <summary>Get name of the layer</summary>
        public string Name => _name;

        /// <inheritdoc/>
        public bool IsVisible { get => _isVisible; set => _isVisible = value; }

        public int SortNumber => _sortNumber;

        public LayerTimingPointList TimingPoints => _timingPoints;

        /// <summary>Get count of alive objects in the current frame.</summary>
        public int ObjectCount => _store.ObjectCount;

        protected ReadOnlySpan<FrameObject> Objects => _store.List;
        protected ReadOnlySpan<FrameObject> AddedObjects => _store.Added;
        protected ReadOnlySpan<FrameObject> RemovedObjects => _store.Removed;
        protected ReadOnlySpan<Renderable> Renderables => _store.Renderables;

        /// <summary>Create new <see cref="Layer"/> with specified name.</summary>
        /// <param name="name">name of the layer</param>
        /// <param name="sortNumber">number for layer sorting</param>
        public Layer(string name, int sortNumber = 0)
        {
            if(name is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(name));
            }
            const int Capacity = 32;
            _name = name;
            _isVisible = true;
            _sortNumber = sortNumber;
            _timingPoints = new LayerTimingPointList(this);
            _store = FrameObjectStore.New(Capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetHostScreen([MaybeNullWhen(false)] out IHostScreen screen)
        {
            screen = _owner?.Screen;
            return screen is not null;
        }

        internal void OnLayerActivatedCallback(LayerCollection owner)
        {
            Debug.Assert(owner is not null);
            Debug.Assert(_owner is null);
            _owner = owner;
            OnLayerActivated();
        }

        internal void OnLayerTerminatedCallback()
        {
            _owner = null;
            _timingPoints.AbortAllEvents();
            OnLayerTerminated();
        }

        internal void AddFrameObject(FrameObject frameObject) => _store.AddFrameObject(frameObject);
        internal void RemoveFrameObject(FrameObject frameObject) => _store.RemoveFrameObject(frameObject);
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

        internal void OnSizeChangedCallback(IHostScreen screen)
        {
            OnSizeChanged(screen);
        }

        protected virtual void RenderOverride(IHostScreen screen)
        {
            var timingPoints = _timingPoints;
            timingPoints.BeforeRendering.DoQueuedEvents();
            SelectMatrix(screen, out var view, out var projection);
            _store.Render(view, projection);
            timingPoints.AfterRendering.DoQueuedEvents();
        }

        protected virtual void SelectMatrix(IHostScreen screen, out Matrix4 view, out Matrix4 projection)
        {
            var camera = screen.Camera;
            view = camera.View;
            projection = camera.Projection;
        }

        protected virtual void OnLayerActivated()
        {
            // nop
        }

        protected virtual void OnLayerTerminated()
        {
            // nop
        }

        protected virtual void OnSizeChanged(IHostScreen screen)
        {
            // nop
        }
    }

    public static class LayerExtension
    {
        public static TLayer Activate<TLayer>(this TLayer layer, IHostScreen screen) where TLayer : Layer
        {
            screen.Layers.Add(layer);
            return layer;
        }

        // TODO: Terminate Layer
    }
}
