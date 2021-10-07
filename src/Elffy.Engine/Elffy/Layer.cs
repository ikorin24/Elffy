#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elffy.Features.Internal;

namespace Elffy
{
    /// <summary>Layer class which has the list of <see cref="FrameObject"/></summary>
    [DebuggerDisplay("Layer: {Name} (ObjectCount = {ObjectCount})")]
    public class Layer : ILayer
    {
        private readonly FrameObjectStore _store;
        private readonly LayerTimingPointList _timingPoints;
        private readonly string _name;
        private LayerCollection? _owner;
        private bool _isVisible;

        // I must set the owner when the layer is added to the LayerCollection. Set null when removed.

        /// <summary>The owner of the layer</summary>
        internal LayerCollection? Owner => _owner;
        LayerCollection? ILayer.OwnerCollection => _owner;

        /// <summary>Get name of the layer</summary>
        public string Name => _name;

        /// <inheritdoc/>
        public bool IsVisible { get => _isVisible; set => _isVisible = value; }

        public LayerTimingPointList TimingPoints => _timingPoints;

        /// <summary>Create new <see cref="Layer"/> with specified name.</summary>
        /// <param name="name">name of the layer</param>
        public Layer(string name)
        {
            if(name is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(name));
            }
            const int Capacity = 32;
            _name = name;
            _isVisible = true;
            _timingPoints = new LayerTimingPointList(this);
            _store = FrameObjectStore.New(Capacity);
        }

        /// <summary>Get count of alive objects in the current frame.</summary>
        public int ObjectCount => _store.ObjectCount;

        internal void OnOwnerChangedCallback(LayerCollection? owner)
        {
            var currentOwner = _owner;
            _owner = owner;
            var layerRemoved = currentOwner is not null && owner is null;
            if(layerRemoved) {
                LayerRemoved();
            }
        }

        internal void AddFrameObject(FrameObject frameObject) => _store.AddFrameObject(frameObject);

        void ILayer.AddFrameObject(FrameObject frameObject) => AddFrameObject(frameObject);

        internal void RemoveFrameObject(FrameObject frameObject) => _store.RemoveFrameObject(frameObject);

        void ILayer.RemoveFrameObject(FrameObject frameObject) => RemoveFrameObject(frameObject);

        internal void ApplyRemove() => _store.ApplyRemove();

        internal void ApplyAdd() => _store.ApplyAdd();

        internal void EarlyUpdate() => _store.EarlyUpdate();

        internal void Update() => _store.Update();

        internal void LateUpdate() => _store.LateUpdate();

        internal void ClearFrameObject() => _store.ClearFrameObject();
        void ILayer.ClearFrameObject() => ClearFrameObject();

        internal void Render(in Matrix4 view, in Matrix4 projection)
        {
            _timingPoints.BeforeRendering.DoQueuedEvents();
            _store.Render(view, projection);
            _timingPoints.AfterRendering.DoQueuedEvents();
        }

        private void LayerRemoved()
        {
            _timingPoints.AbortAllEvents();
        }
    }
}
