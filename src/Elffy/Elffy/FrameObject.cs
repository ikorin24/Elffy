#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Elffy.Threading;
using Cysharp.Threading.Tasks;
using System.Threading;
using Elffy.UI;
using Elffy.Features.Internal;

namespace Elffy
{
    /// <summary>The base class which is controlled by the engine. It provides frame update operations and operations to be managed by the engine.</summary>
    public abstract class FrameObject
    {
        private IHostScreen? _hostScreen;
        private ILayer? _layer;
        private LifeState _state = LifeState.New;
        private bool _isFrozen;

        /// <summary>Event of activated</summary>
        public event Action<FrameObject>? Activated;

        /// <summary>Event of terminated, which fires when <see cref="Terminate"/> is called.</summary>
        public event Action<FrameObject>? Terminated;

        /// <summary>Event of alive, which fires in the first frame where <see cref="LifeState"/> get <see cref="LifeState.Alive"/>.</summary>
        public event Action<FrameObject>? Alive;

        /// <summary>Event of dead, which fires in the first frame where <see cref="LifeState"/> get <see cref="LifeState.Dead"/>.</summary>
        public event Action<FrameObject>? Dead;

        /// <summary>Event of early updating</summary>
        public event Action<FrameObject>? EarlyUpdated;

        /// <summary>Event of updating</summary>
        public event Action<FrameObject>? Updated;

        /// <summary>Event of late updating</summary>
        public event Action<FrameObject>? LateUpdated;

        /// <summary>Get life state of <see cref="FrameObject"/></summary>
        public LifeState LifeState => _state;

        /// <summary>Get or set whether the <see cref="FrameObject"/> skips early updating, updating, and late updating. (Skips if true)</summary>
        public bool IsFrozen { get => _isFrozen; set => _isFrozen = value; }

        /// <summary>Get the layer where the <see cref="FrameObject"/> is.</summary>
        private protected ILayer? InternalLayer => _layer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetLayer([MaybeNullWhen(false)] out Layer layer)
        {
            // [NOTE]
            // DON'T call this method if the object on the internal layer. (e.g. UIRenderable.)
            // Use 'InternalLayer' property instead.

            layer = SafeCast.As<Layer>(_layer);
            return layer is not null;
        }

        /// <summary>Try to get HostScreen of the <see cref="FrameObject"/></summary>
        /// <param name="screen">HostScreen of the <see cref="FrameObject"/></param>
        /// <returns>success or not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetHostScreen([MaybeNullWhen(false)] out IHostScreen screen)
        {
            if(_hostScreen is null) {
                screen = null;
                return false;
            }
            else {
                screen = _hostScreen;
                return true;
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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public UniTask<FrameObject> Activate(Layer layer, CancellationToken cancellationToken = default)
        {
            return Activate(layer, FrameTiming.Update, cancellationToken);
        }

        /// <summary>Activate the object in the specified layer.</summary>
        /// <param name="layer">layer where the object is activated</param>
        /// <param name="timing"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async UniTask<FrameObject> Activate(Layer layer, FrameTiming timing, CancellationToken cancellationToken = default)
        {
            if(layer is null) { ThrowNullArg(); }
            var screen = GetHostScreen(layer);
            if(screen is null) {
                ThrowInvalidLayer();
            }
            if(Engine.CurrentContext != screen) {
                ThrowContextMismatch();
            }

            timing.ThrowArgExceptionIfNotSpecified(nameof(timing));
            cancellationToken.ThrowIfCancellationRequested();

            if(_state.IsAfter(LifeState.New)) {
                if(_state == LifeState.Activating) {
                    throw new InvalidOperationException($"Cannot call {nameof(Activate)} method when the life state is {LifeState.Activating}.");
                }
                await screen.TimingPoints.TimingOf(timing).Ensure();
                return this;
            }

            Debug.Assert(_state == LifeState.New);
            _hostScreen = screen;
            _layer = layer;
            try {
                _state = LifeState.Activating;
                await OnActivating(cancellationToken);
            }
            catch {
                // If exceptions throw on activating, terminate the object if possible.

                if(screen.RunningToken.IsCancellationRequested == false) {
                    await screen.TimingPoints.Update.Ensure(CancellationToken.None);
                    try {
                        Terminate();
                    }
                    catch {
                        // Ignore exceptions in Terminate()
                    }
                }

                throw;  // Throw exceptions of activating.
            }
            await screen.TimingPoints.TimingOf(timing).Ensure(cancellationToken);
            _state = LifeState.Activated;
            layer.AddFrameObject(this);
            OnActivated();
            return this;

            [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(layer));
            [DoesNotReturn] static void ThrowInvalidLayer() => throw new ArgumentException($"{nameof(layer)} is not associated with {nameof(IHostScreen)}");
            [DoesNotReturn] static void ThrowContextMismatch() => throw new InvalidOperationException("Invalid current context.");
        }

        internal void ActivateOnInternalLayer(ILayer layer)
        {
            if(layer is null) { ThrowNullArg(); }
            if(_state != LifeState.New) { return; }
            Debug.Assert(layer is Layer == false, $"'{typeof(Layer)}' type can't pass here. Where are you from ?");
            Debug.Assert(layer.OwnerCollection is null == false);
            Debug.Assert(GetType() == typeof(UIRenderable));

            var screen = GetHostScreen(layer);
            Debug.Assert(screen is not null);
            Debug.Assert(Engine.CurrentContext == screen);

            _hostScreen = screen;
            _state = LifeState.Activating;
            _layer = layer;

            // [NOTE] UIRenderable must complete OnActivating synchronously. (See the implementation of UIRenderable)
            OnActivating(default).SyncGetResult();

            _state = LifeState.Activated;
            layer.AddFrameObject(this);

            OnActivated();

            [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(layer));
        }

        /// <summary>Terminate the object and remove it from the engine.</summary>
        public void Terminate()
        {
            var context = Engine.CurrentContext;
            if(context is null || context != _hostScreen) {
                ThrowContextMismatch();
            }
            if(_state == LifeState.New || _state.IsSameOrAfter(LifeState.Terminated)) {
                return;
            }

            Debug.Assert(_layer is not null);

            _state = LifeState.Terminated;
            _layer.RemoveFrameObject(this);
            OnTerminated();

            [DoesNotReturn] static void ThrowContextMismatch() => throw new InvalidOperationException("Invalid current context.");
        }

        protected virtual void OnEarlyUpdate() => EarlyUpdated?.Invoke(this);

        protected virtual void OnUpdate() => Updated?.Invoke(this);

        protected virtual void OnLateUpdte() => LateUpdated?.Invoke(this);

        protected virtual UniTask<AsyncUnit> OnActivating(CancellationToken cancellationToken) => UniTask.FromResult(AsyncUnit.Default);

        protected virtual void OnActivated() => Activated?.Invoke(this);

        protected virtual void OnTerminated() => Terminated?.Invoke(this);

        protected virtual void OnAlive() => Alive?.Invoke(this);

        protected virtual void OnDead() => Dead?.Invoke(this);

        internal void AddToObjectStoreCallback()
        {
            Debug.Assert(_state == LifeState.Activated);
            _state = LifeState.Alive;
            OnAlive();
        }

        internal void RemovedFromObjectStoreCallback()
        {
            Debug.Assert(_hostScreen is not null);
            Debug.Assert(Engine.CurrentContext == _hostScreen);
            Debug.Assert(_state == LifeState.Terminated);
            _state = LifeState.Dead;
            _layer = null;
            _hostScreen = null;
            OnDead();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IHostScreen? GetHostScreen(ILayer? layer)
        {
            return layer?.OwnerCollection?.OwnerRenderingArea.OwnerScreen;
        }
    }
}
