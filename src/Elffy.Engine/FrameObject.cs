#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.UI;
using Elffy.Threading;
using System.Runtime.ExceptionServices;

namespace Elffy
{
    /// <summary>The base class which is controlled by the engine. It provides frame update operations and operations to be managed by the engine.</summary>
    public abstract class FrameObject
    {
        private IHostScreen? _hostScreen;
        private Layer? _layer;
        private AsyncEventRaiser<FrameObject>? _activating;
        private AsyncEventRaiser<FrameObject>? _terminating;
        private LifeState _state = LifeState.New;
        private bool _isFrozen;
        private FrameObjectInstanceType _instanceType = FrameObjectInstanceType.FrameObject;

        public AsyncEvent<FrameObject> Activating => new(ref _activating);
        public AsyncEvent<FrameObject> Terminating => new(ref _terminating);

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

        public IHostScreen? Screen => _hostScreen;
        public Layer? Layer => _layer;

        public FrameObject()
        {
        }

        private protected FrameObject(FrameObjectInstanceType instanceType)
        {
            _instanceType = instanceType;
#if DEBUG
            Debug.Assert(_instanceType == this switch
            {
                Renderable => FrameObjectInstanceType.Renderable,
                Positionable => FrameObjectInstanceType.Positionable,
                ComponentOwner => FrameObjectInstanceType.ComponentOwner,
                FrameObject => FrameObjectInstanceType.FrameObject,
            });
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetLayer([MaybeNullWhen(false)] out Layer layer)
        {
            layer = _layer;
            return layer is not null;
        }

        /// <summary>Try to get HostScreen of the <see cref="FrameObject"/></summary>
        /// <param name="screen">HostScreen of the <see cref="FrameObject"/></param>
        /// <returns>success or not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetScreen([MaybeNullWhen(false)] out IHostScreen screen)
        {
            screen = _hostScreen;
            return screen is not null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IHostScreen GetValidScreen()
        {
            var screen = _hostScreen;
            if(screen is null) { ThrowHelper.ThrowInvalidNullScreen(); }
            return screen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsComponentOwner() => _instanceType >= FrameObjectInstanceType.ComponentOwner;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsComponentOwner([MaybeNullWhen(false)] out ComponentOwner componentOwner)
        {
            if(IsComponentOwner()) {
                componentOwner = SafeCast.As<ComponentOwner>(this);
                return true;
            }
            componentOwner = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsPositionable() => _instanceType >= FrameObjectInstanceType.Positionable;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsPositionable([MaybeNullWhen(false)] out Positionable positionable)
        {
            if(IsPositionable()) {
                positionable = SafeCast.As<Positionable>(this);
                return true;
            }
            positionable = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsRenderable() => _instanceType >= FrameObjectInstanceType.Renderable;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsRenderable([MaybeNullWhen(false)] out Renderable renderable)
        {
            if(IsRenderable()) {
                renderable = SafeCast.As<Renderable>(this);
                return true;
            }
            renderable = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EarlyUpdate() => OnEarlyUpdate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update() => OnUpdate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LateUpdate() => OnLateUpdte();

        internal UniTask ActivateOnLayer(Layer layer, FrameTimingPoint timingPoint, CancellationToken ct)
        {
            var screen = layer.Screen;
            CheckStateAndThreadForActivation(screen, layer, timingPoint);
            ct.ThrowIfCancellationRequested();
            return ActivateOnLayerWithoutCheck(layer, timingPoint, screen, ct);
        }

        internal void CheckStateAndThreadForActivation([NotNull] IHostScreen? screen, [NotNull] Layer? layer, [NotNull] FrameTimingPoint? timingPoint)
        {
            ArgumentNullException.ThrowIfNull(layer);
            ArgumentNullException.ThrowIfNull(timingPoint);
            ArgumentNullException.ThrowIfNull(screen);
            if(layer.Screen != screen) {
                throw new ArgumentException($"The layer is not associated with the specified {nameof(IHostScreen)}.");
            }
            var currentContext = Engine.CurrentContext;
            if(currentContext != screen) {
                ContextMismatchException.Throw(currentContext, screen);
            }
            if(_state.IsAfter(LifeState.New)) {
                throw new InvalidOperationException($"Cannot activate the {nameof(FrameObject)} twice.");
            }
        }

        internal async UniTask ActivateOnLayerWithoutCheck(Layer layer, FrameTimingPoint timingPoint, IHostScreen screen, CancellationToken ct)
        {
            Debug.Assert(_state == LifeState.New);
            Debug.Assert(_hostScreen is null);
            _hostScreen = screen;
            _layer = layer;
            _state = LifeState.Activating;
            ExceptionDispatchInfo? edi = null;
            try {
                await _activating.RaiseIfNotNull(this, ct);
                _activating?.Clear();
            }
            catch(Exception ex) {
                if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) {
                    edi = ExceptionDispatchInfo.Capture(ex);
                }
            }

            // TODO: Consider the case that screen is already dead.
            if(screen.CurrentTiming.IsOutOfFrameLoop()) {
                await screen.TimingPoints.FrameInitializing.Next(ct);
            }
            layer.AddFrameObject(this);
            await timingPoint.NextFrame(ct);
            Debug.Assert(_state.IsSameOrAfter(LifeState.Alive));
            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) {
                edi?.Throw();
            }
            return;
        }

        internal async UniTask TerminateFromLayerWithoutCheck(Layer layer, FrameTimingPoint timingPoint, IHostScreen screen)
        {
            _state = LifeState.Terminating;
            try {
                await _terminating.RaiseIfNotNull(this, CancellationToken.None);
            }
            catch {
                if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
            }
            finally {
                layer.RemoveFrameObject(this);
            }
            Debug.Assert(screen is not null);
            await timingPoint.NextFrame(CancellationToken.None);
            Debug.Assert(_state == LifeState.Dead);
        }

        internal UniTask TerminateFromLayer(FrameTimingPoint? timingPoint)
        {
            var context = Engine.CurrentContext;
            var screen = _hostScreen;
            if(screen is null) {
                if(_state == LifeState.New) { ThrowNotActivated(); }
                if(_state == LifeState.Dead) { throw new InvalidOperationException($"Cannot terminate {nameof(FrameObject)} because it is already dead."); }
                Debug.Fail("Something wrong.");
            }
            if(context != screen) {
                ContextMismatchException.Throw(context, screen);
            }
            if(_state == LifeState.Activating) {
                throw new InvalidOperationException($"Cannot terminate {nameof(FrameObject)} when activating.");
            }
            if(_state.IsSameOrAfter(LifeState.Terminating)) { ThrowTerminateTwice(); }
            Debug.Assert(timingPoint is not null);
            Debug.Assert(_state == LifeState.Alive);
            Debug.Assert(_layer is not null);

            return TerminateFromLayerWithoutCheck(_layer, timingPoint ?? screen.TimingPoints.Update, screen);

            [DoesNotReturn] static void ThrowNotActivated() => throw new InvalidOperationException($"Cannot terminate {nameof(FrameObject)} because it is not activated.");
            [DoesNotReturn] static void ThrowTerminateTwice() => throw new InvalidOperationException($"Cannot terminate {nameof(FrameObject)} twice.");
        }

        protected virtual void OnEarlyUpdate() => EarlyUpdated?.Invoke(this);

        protected virtual void OnUpdate() => Updated?.Invoke(this);

        protected virtual void OnLateUpdte() => LateUpdated?.Invoke(this);

        //protected virtual UniTask OnTerminating() => UniTask.CompletedTask;

        protected virtual void OnAlive() => Alive?.Invoke(this);

        protected virtual void OnDead() => Dead?.Invoke(this);

        internal void AddToObjectStoreCallback()
        {
            Debug.Assert(_state == LifeState.Activating);
            _state = LifeState.Alive;
            OnAlive();
        }

        internal void RemovedFromObjectStoreCallback()
        {
            Debug.Assert(_hostScreen is not null);
            Debug.Assert(Engine.CurrentContext == _hostScreen);
            Debug.Assert(_state == LifeState.Terminating);
            _state = LifeState.Dead;
            _layer = null;
            _hostScreen = null;
            OnDead();
        }

        private protected enum FrameObjectInstanceType : byte
        {
            FrameObject = 0,
            ComponentOwner = 1,
            Positionable = 2,
            Renderable = 3,
        }
    }

    public static class FrameObjectActivationExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask<T> Activate<T>(this T source, WorldLayer layer, CancellationToken cancellationToken = default)
            where T : FrameObject
        {
            if(layer.TryGetScreen(out var screen) == false) {
                ThrowInvalidLayer();
            }
            await source.ActivateOnLayer(layer, screen.TimingPoints.Update, cancellationToken);
            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask<T> Activate<T>(this T frameObject, WorldLayer layer, FrameTimingPoint timingPoint, CancellationToken cancellationToken = default)
            where T : FrameObject
        {
            await frameObject.ActivateOnLayer(layer, timingPoint, cancellationToken);
            return frameObject;
        }

        public static async UniTask<T> Activate<T>(this T frameObject, WorldLayer layer, FrameTiming timing, CancellationToken cancellationToken = default)
            where T : FrameObject
        {
            if(layer.TryGetScreen(out var screen) == false) {
                ThrowInvalidLayer();
            }
            var timingPoint = screen.TimingPoints.TimingOf(timing);
            await frameObject.ActivateOnLayer(layer, timingPoint, cancellationToken);
            return frameObject;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<T> Terminate<T>(this T frameObject) where T : FrameObject
        {
            return frameObject.Terminate(FrameTiming.Update);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask<T> Terminate<T>(this T frameObject, FrameTimingPoint timingPoint) where T : FrameObject
        {
            if(timingPoint is null) {
                ThrowNullArg(nameof(timingPoint));
            }
            await frameObject.TerminateFromLayer(timingPoint);
            return frameObject;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask<T> Terminate<T>(this T frameObject, FrameTiming timing) where T : FrameObject
        {
            var timingPoint = frameObject.TryGetScreen(out var screen) ? screen.TimingPoints.TimingOf(timing) : null;
            await frameObject.TerminateFromLayer(timingPoint);
            return frameObject;
        }

        [DoesNotReturn]
        private static void ThrowInvalidLayer() => throw new ArgumentException($"The layer is not associated with {nameof(IHostScreen)}");

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);
    }
}
