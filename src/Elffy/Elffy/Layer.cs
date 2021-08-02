#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elffy.Core;

namespace Elffy
{
    /// <summary>Layer class which has the list of <see cref="FrameObject"/></summary>
    [DebuggerDisplay("Layer: {Name} (ObjectCount = {ObjectCount})")]
    public class Layer : ILayer
    {
        private readonly FrameObjectStore _store;
        private LayerCollection? _owner;
        private readonly string _name;
        private bool _isVisible;

        // I must set the owner when the layer is added to the LayerCollection. Set null when removed.

        /// <summary>The owner of the layer</summary>
        internal LayerCollection? Owner { get => _owner; set => _owner = value; }
        LayerCollection? ILayer.OwnerCollection => Owner;

        /// <summary>Get name of the layer</summary>
        public string Name => _name;

        /// <inheritdoc/>
        public bool IsVisible { get => _isVisible; set => _isVisible = value; }

        /// <summary>Create new <see cref="Layer"/> with specified name.</summary>
        /// <param name="name">name of the layer</param>
        public Layer(string name) : this(name, 32)
        {
        }

        /// <summary>Create new <see cref="Layer"/> with specified name.</summary>
        /// <param name="name">name of the layer</param>
        /// <param name="capacityHint">capacity hint</param>
        public Layer(string name, int capacityHint)
        {
            if(name is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(name));
            }
            capacityHint = Math.Max(0, capacityHint);
            _name = name;
            _isVisible = true;
            _store = FrameObjectStore.New(capacityHint);
        }

        /// <summary>Get count of alive objects in the current frame.</summary>
        public int ObjectCount => _store.ObjectCount;

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

        internal void Render(in Matrix4 view, in Matrix4 projection) => _store.Render(view, projection);
    }
}
