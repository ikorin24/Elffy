﻿#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Elffy.Features.Internal;
using Elffy.Graphics.OpenGL;

namespace Elffy
{
    [DebuggerTypeProxy(typeof(LayerCollectionDebuggerTypeProxy))]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class LayerCollection
    {
        private readonly LazyApplyingList<Layer> _list;
        private readonly RenderingArea _owner;

        internal IHostScreen Screen => _owner.OwnerScreen;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"{nameof(LayerCollection)} (Count = {_list.Count})";

        internal LayerCollection(RenderingArea owner)
        {
            _list = new();
            _owner = owner;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Add(Layer layer)
        {
            Debug.Assert(layer is not null);
            Debug.Assert(layer.Owner == this);
            Debug.Assert(layer.LifeState == LayerLifeState.Activating);

            _list.Add(layer, addedLayer =>  // [capture] this
            {
                addedLayer.OnAddedToListCallback(this);
                addedLayer.OnSizeChangedCallback(Screen);
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Remove(Layer layer)
        {
            if(layer is null) { ThrowNullArg(nameof(layer)); }
            _list.Remove(layer, removedLayer =>
            {
                removedLayer.OnLayerTerminatedCallback();
            });
        }

        internal void TerminateAllImmediately()
        {
            // Clear all objects in all layers.
            foreach(var layer in AsSpan()) {
                layer.ClearFrameObject();
            }
            // Terminate all layers immediately.
            foreach(var layer in AsSpan()) {
                layer.OnLayerTerminatedCallback();
            }
            _list.Clear();
        }

        internal void NotifySizeChanged()
        {
            var screen = Screen;
            foreach(var layer in AsSpan()) {
                layer.OnSizeChangedCallback(screen);
            }
        }

        internal void ApplyAdd()
        {
            var applied = _list.ApplyAdd();
            if(applied) {
                _list.AsSpan().Sort(static (l1, l2) => l1.SortNumber - l2.SortNumber);
            }
            foreach(var layer in AsSpan()) {
                layer.ApplyAdd();
            }
        }

        internal void ApplyRemove()
        {
            foreach(var layer in AsSpan()) {
                layer.ApplyRemove();
            }
            _list.ApplyRemove();
        }

        internal void EarlyUpdate()
        {
            foreach(var layer in AsSpan()) {
                layer.EarlyUpdate();
            }
        }

        internal void Update()
        {
            foreach(var layer in AsSpan()) {
                layer.Update();
            }
        }

        internal void LateUpdate()
        {
            foreach(var layer in AsSpan()) {
                layer.LateUpdate();
            }
        }

        internal void Render()
        {
            var screen = Screen;
            var fbo = FBO.Empty;
            foreach(var layer in AsSpan()) {
                layer.Render(screen, ref fbo);
            }
        }

        private ReadOnlySpan<Layer> AsSpan() => _list.AsReadOnlySpan();

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);

        [DoesNotReturn]
        private static void ThrowAlreadyOwned(string message) => throw new InvalidOperationException(message);

        internal class LayerCollectionDebuggerTypeProxy
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly LayerCollection _entity;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public Layer[] Layers => _entity._list.AsReadOnlySpan().ToArray();

            public LayerCollectionDebuggerTypeProxy(LayerCollection entity) => _entity = entity;
        }
    }
}