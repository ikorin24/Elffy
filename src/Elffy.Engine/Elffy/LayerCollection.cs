#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Elffy.Features.Internal;
using Elffy.UI;

namespace Elffy
{
    /// <summary>List class of <see cref="Layer"/></summary>
    [DebuggerTypeProxy(typeof(LayerCollectionDebuggerTypeProxy))]
    [DebuggerDisplay("LayerCollection (Count = {Count})")]
    public sealed class LayerCollection
    {
        private const string DefaultLayerName = "Default";

        private readonly LazyApplyingList<ILayer> _list;
        private readonly RenderingArea _owner;
        private readonly UILayer _uiLayer;
        private readonly Layer _defaultLayer;

        internal RenderingArea OwnerRenderingArea => _owner;

        /// <summary>Get UI layer instance.</summary>
        /// <remarks>DO NOT make it public. This is not in the list, don't share the instance as public.</remarks>
        internal UILayer UILayer => _uiLayer;

        /// <summary>Get world layer instance. (This instance is in the list.)</summary>
        public Layer DefaultLayer => _defaultLayer;

        /// <summary>Get layer count</summary>
        public int Count => _list.Count;

        internal LayerCollection(RenderingArea owner)
        {
            _list = LazyApplyingList<ILayer>.New();
            _owner = owner;
            _uiLayer = new UILayer(this);
            _defaultLayer = new Layer(DefaultLayerName);
            AddDefaltLayers();
        }

        /// <summary>Add <see cref="Layer"/></summary>
        /// <param name="layer">layer instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Layer layer)
        {
            if(layer is null) { ThrowNullArg(nameof(layer)); }
            if(layer.Owner is null == false) { ThrowAlreadyOwned($"Layer is already owned by {nameof(LayerCollection)}."); }
            layer.OnOwnerChangedCallback(this);
            _list.Add(layer);
        }

        /// <summary>Clear all layers. (Default layer is not cleared.)</summary>
        internal void Clear()
        {
            foreach(var layer in _list.AsSpan()) {
                (layer as Layer)?.OnOwnerChangedCallback(null);
            }
            _list.Clear();
            AddDefaltLayers();
        }

        /// <summary>Remove <see cref="Layer"/></summary>
        /// <param name="layer">layer</param>
        /// <returns>true if success, false when not contains</returns>
        public void Remove(Layer layer)
        {
            if(layer is null) { ThrowNullArg(nameof(layer)); }
            _list.Remove(layer);
        }

        internal void ApplyAdd()
        {
            _list.ApplyAdd();

            // TODO: Sort layers

            foreach(var layer in AsSpan()) {
                layer.ApplyAdd();
            }
        }

        internal void ApplyRemove()
        {
            foreach(var layer in AsSpan()) {
                layer.ApplyRemove();
            }
            _list.ApplyRemove(removedLayer => (removedLayer as Layer)?.OnOwnerChangedCallback(null));
        }

        internal ReadOnlySpan<ILayer> AsSpan() => _list.AsSpan();

        private void AddDefaltLayers()
        {
            Add(_defaultLayer);
            _list.Add(_uiLayer);
        }

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);

        [DoesNotReturn]
        private static void ThrowAlreadyOwned(string message) => throw new InvalidOperationException(message);

        internal class LayerCollectionDebuggerTypeProxy
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly LayerCollection _entity;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public ILayer[] Layers => _entity._list.AsSpan().ToArray();

            public LayerCollectionDebuggerTypeProxy(LayerCollection entity) => _entity = entity;
        }
    }
}
