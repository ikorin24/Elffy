#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Effective;
using Elffy.Features.Internal;
using Elffy.UI;

namespace Elffy
{
    /// <summary>List class of <see cref="Layer"/></summary>
    [DebuggerTypeProxy(typeof(LayerCollectionDebuggerTypeProxy))]
    [DebuggerDisplay("LayerCollection (Count = {Count})")]
    public sealed class LayerCollection : IReadOnlyList<Layer>, IReadOnlyCollection<Layer>
    {
        private const string WorldLayerName = "World";
        private readonly List<Layer> _list = new List<Layer>();

        internal RenderingArea OwnerRenderingArea { get; }

        /// <summary>Get UI layer instance.</summary>
        /// <remarks>DO NOT make it public. This is not in the list, don't share the instance as public.</remarks>
        internal UILayer UILayer { get; }

        /// <summary>Get world layer instance. (This instance is in the list.)</summary>
        public Layer WorldLayer { get; }

        /// <summary>Get layer count</summary>
        public int Count => _list.Count;

        /// <summary>Get <see cref="Layer"/> of specified index</summary>
        /// <param name="index">index to get a layer</param>
        /// <returns><see cref="Layer"/> instance</returns>
        public Layer this[int index] => _list[index];   // no index bounds checking (List<T> checks it.)

        internal LayerCollection(RenderingArea owner)
        {
            OwnerRenderingArea = owner;
            UILayer = new UILayer(this);
            WorldLayer = new Layer(WorldLayerName, 256);
            AddDefaltLayers();
        }

        /// <summary>Add <see cref="Layer"/></summary>
        /// <param name="layer">layer instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Layer layer)
        {
            if(layer is null) {
                ThrowNullArg();
                static void ThrowNullArg() => throw new ArgumentNullException(nameof(layer));
            }
            if(layer!.Owner is null == false) {
                ThrowAlreadyOwned();
                static void ThrowAlreadyOwned() => throw new InvalidOperationException($"Layer is already owned by {nameof(LayerCollection)}.");
            }
            layer.Owner = this;
            _list.Add(layer);
        }

        /// <summary>Clear all layers. (Default layer is not cleared.)</summary>
        public void Clear()
        {
            foreach(var layer in _list.AsSpan()) {
                layer.Owner = null;
            }
            _list.Clear();
            AddDefaltLayers();
        }

        /// <summary>Remove <see cref="Layer"/></summary>
        /// <param name="layer">layer</param>
        /// <returns>true if success, false when not contains</returns>
        public bool Remove(Layer layer)
        {
            if(layer is null) {
                ThrowNullArg();
                static void ThrowNullArg() => throw new ArgumentNullException(nameof(layer));
            }
            var removed = _list.Remove(layer!);
            if(removed) {
                layer!.Owner = null;
            }
            return removed;
        }

        internal ReadOnlySpan<Layer> AsReadOnlySpan() => _list.AsReadOnlySpan();

        public List<Layer>.Enumerator GetEnumerator() => _list.GetEnumerator();

        IEnumerator<Layer> IEnumerable<Layer>.GetEnumerator() => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();


        private void AddDefaltLayers()
        {
            Add(WorldLayer);
        }


        #region class LayerCollectionDebuggerTypeProxy
        internal class LayerCollectionDebuggerTypeProxy
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly LayerCollection _entity;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public Layer[] Layers
            {
                get
                {
                    var layers = new Layer[_entity.Count];
                    _entity._list.CopyTo(layers, 0);
                    return layers;
                }
            }

            public LayerCollectionDebuggerTypeProxy(LayerCollection entity) => _entity = entity;
        }
        #endregion class LayerCollectionDebuggerTypeProxy<T>
    }
}
