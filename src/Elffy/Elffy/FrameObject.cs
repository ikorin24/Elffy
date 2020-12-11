#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Elffy.Core;
using Elffy.AssemblyServices;

namespace Elffy
{
    /// <summary>The base class which is controlled by the engine. It provides frame update operations and operations to be managed by the engine.</summary>
    public abstract class FrameObject
    {
        private IHostScreen? _hostScreen;
        private ILayer? _layer;
        private object? _tag;
        private FrameObjectLifeState _state = FrameObjectLifeState.New;
        private bool _isFrozen;

        /// <summary>Event of activated, which fires when <see cref="Activate(Layer)"/> is called.</summary>
        public event ActionEventHandler<FrameObject>? Activated;

        /// <summary>Event of terminated, which fires when <see cref="Terminate"/> is called.</summary>
        public event ActionEventHandler<FrameObject>? Terminated;

        /// <summary>Event of alive, which fires in the first frame where <see cref="LifeState"/> get <see cref="FrameObjectLifeState.Alive"/>.</summary>
        public event ActionEventHandler<FrameObject>? Alive;

        /// <summary>Event of dead, which fires in the first frame where <see cref="LifeState"/> get <see cref="FrameObjectLifeState.Dead"/>.</summary>
        public event ActionEventHandler<FrameObject>? Dead;

        /// <summary>Event of early updating</summary>
        public event ActionEventHandler<FrameObject>? EarlyUpdated;

        /// <summary>Event of updating</summary>
        public event ActionEventHandler<FrameObject>? Updated;

        /// <summary>Event of late updating</summary>
        public event ActionEventHandler<FrameObject>? LateUpdated;

        /// <summary>Get life state of <see cref="FrameObject"/></summary>
        public FrameObjectLifeState LifeState => _state;

        public bool IsNew => _state == FrameObjectLifeState.New;

        public bool IsActivated => _state == FrameObjectLifeState.Activated;

        public bool IsAlive => _state == FrameObjectLifeState.Alive;

        public bool IsTerminated => _state == FrameObjectLifeState.Terminated;

        public bool IsDead => _state == FrameObjectLifeState.Dead;

        /// <summary>Get or set whether the <see cref="FrameObject"/> skips early updating, updating, and late updating. (Skips if true)</summary>
        public bool IsFrozen { get => _isFrozen; set => _isFrozen = value; }

        /// <summary>Get or set the tag.</summary>
        /// <remarks>The engine does not use the tag for any purpose.</remarks>
        public object? Tag { get => _tag; set => _tag = value; }

        /// <summary>Get the layer where the <see cref="FrameObject"/> is.</summary>
        private protected ILayer? InternalLayer => _layer;

        // [NOTE]
        // DON'T call this property of the object which is on the internal layer. (e.g. UIRenderable.)
        // Use 'InternalLayer' property instead.

        /// <summary>Get the layer where the <see cref="FrameObject"/> is.</summary>
        /// <exception cref="InvalidOperationException"> <see cref="FrameObject"/> is not activated yet or already dead.</exception>
        public Layer Layer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AssemblyState.IsDebug ? (Layer?)_layer ?? throw new InvalidOperationException()
                                         : Unsafe.As<Layer?>(_layer) ?? throw new InvalidOperationException();

            // ↑ Use cast in the debug build. The branch is removed by JIT.
        }


        /// <summary>Get HostScreen of this <see cref="FrameObject"/>.</summary>
        /// <exception cref="InvalidOperationException"> <see cref="FrameObject"/> is not activated yet or already dead.</exception>
        public IHostScreen HostScreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _hostScreen ?? TryGetScreen(_layer);

                [MethodImpl(MethodImplOptions.NoInlining)]
                static IHostScreen TryGetScreen(ILayer? layer) => GetHostScreen(layer) ?? throw new InvalidOperationException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EarlyUpdate() => OnEarlyUpdate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update() => OnUpdate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LateUpdate() => OnLateUpdte();

        /// <summary>Activate the object in the specified layer.</summary>
        /// <param name="layer">layer where the object is activated</param>
        public void Activate(Layer layer)
        {
            if(layer is null) { ThrowNullArg(); }
            if(_state != FrameObjectLifeState.New) { return; }

            var screen = GetHostScreen(layer);
            if(screen is null) {
                ThrowInvalidLayer();
            }
            screen.ThrowIfNotMainThread();
            _hostScreen = screen;

            _state = FrameObjectLifeState.Activated;
            _layer = layer;
            layer!.AddFrameObject(this);
            OnActivated();

            [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(layer));
            [DoesNotReturn] static void ThrowInvalidLayer() => throw new ArgumentException($"{nameof(layer)} is not associated with {nameof(IHostScreen)}");
        }

        internal void Activate<TLayer>(TLayer layer) where TLayer : class, ILayer
        {
            if(layer is null) { ThrowNullArg(); }
            if(_state != FrameObjectLifeState.New) { return; }

            Debug.Assert(layer is Layer == false, $"'{typeof(Layer)}' type can't pass here. Where are you from ?");
            Debug.Assert(layer!.OwnerCollection is null == false);
            Debug.Assert(GetHostScreen(layer)!.IsThreadMain());

            _state = FrameObjectLifeState.Activated;
            _layer = layer;
            layer.AddFrameObject(this);
            OnActivated();

            [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(layer));
        }

        /// <summary>Terminate the object and remove it from the engine.</summary>
        public void Terminate()
        {
            HostScreen.ThrowIfNotMainThread();

            if(_state != FrameObjectLifeState.Activated && _state != FrameObjectLifeState.Alive) {
                return;
            }

            Debug.Assert(_layer is not null);

            _state = FrameObjectLifeState.Terminated;
            _layer.RemoveFrameObject(this);
            OnTerminated();
        }

        protected virtual void OnEarlyUpdate() => EarlyUpdated?.Invoke(this);

        protected virtual void OnUpdate() => Updated?.Invoke(this);

        protected virtual void OnLateUpdte() => LateUpdated?.Invoke(this);

        protected virtual void OnActivated() => Activated?.Invoke(this);

        protected virtual void OnTerminated() => Terminated?.Invoke(this);

        protected virtual void OnAlive() => Alive?.Invoke(this);

        protected virtual void OnDead() => Dead?.Invoke(this);

        internal void AddToObjectStoreCallback()
        {
            Debug.Assert(_state == FrameObjectLifeState.Activated);
            _state = FrameObjectLifeState.Alive;
            OnAlive();
        }

        internal void RemovedFromObjectStoreCallback()
        {
            Debug.Assert(_state == FrameObjectLifeState.Terminated);
            _state = FrameObjectLifeState.Dead;
            _layer = null;
            _hostScreen = null;
            OnDead();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IHostScreen? GetHostScreen<TLayer>(TLayer? layer) where TLayer : class, ILayer
        {
            return layer?.OwnerCollection?.OwnerRenderingArea.OwnerScreen;
        }
    }
}
