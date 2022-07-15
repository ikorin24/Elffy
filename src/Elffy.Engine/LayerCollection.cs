#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Elffy.Effective;
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
        internal void Add(Layer layer, Action<Layer> onAdded)
        {
            Debug.Assert(layer is not null);
            Debug.Assert(onAdded is not null);
            Debug.Assert(layer.Owner == this);
            Debug.Assert(layer.LifeState == LayerLifeState.Activating);

            _list.Add(layer, onAdded);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Remove(Layer layer, Action<Layer> onRemoved)
        {
            if(layer is null) { ThrowNullArg(nameof(layer)); }
            _list.Remove(layer, onRemoved);
        }

        internal void TerminateAllLayers<T>(T state, Action<T> onDead)
        {
            var layers = AsSpan();
            var tasks = new UniTask<Layer>[layers.Length];
            for(int i = 0; i < tasks.Length; i++) {
                var layer = layers[i];
                try {
                    tasks[i] = layer.Terminate(FrameTiming.NotSpecified);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // ignore exceptions
                    tasks[i] = UniTask.FromResult(layer);
                }
            }
            TerminateAllPrivate(UniTask.WhenAll(tasks), state, onDead);
            return;

            static async void TerminateAllPrivate(UniTask<Layer[]> task, T state, Action<T> onDead)
            {
                try {
                    await task;
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // ignore exceptions
                }
                finally {
                    onDead(state);
                }
            }
        }

        internal void AbortAllLayers()
        {
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

            // render shadow to shadow maps
            var lights = screen.Lights;
            var shadowMaps = lights.GetShadowMaps();
            var lightMatrices = lights.GetMatrices();
            for(int i = 0; i < shadowMaps.Length; i++) {
                FBO.Bind(shadowMaps[i].Fbo, FBO.Target.FrameBuffer);
                foreach(var layer in AsSpan()) {
                    layer.RenderShadowMap(screen, lightMatrices[i]);
                }
            }

            // render objects
            var fbo = FBO.Empty;
            foreach(var layer in AsSpan()) {
                layer.Render(screen, ref fbo);
            }
        }

        public ReadOnlySpan<Layer> AsSpan() => _list.AsReadOnlySpan();

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
