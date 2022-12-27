#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Runtime.ExceptionServices;
using Cysharp.Threading.Tasks;
using Elffy.Threading;

namespace Elffy
{
    /// <summary>The base class which is controlled by the engine. It provides frame update operations and operations to be managed by the engine.</summary>
    [DebuggerDisplay("{GetType().Name,nq} (Name = {Name})")]
    public abstract class FrameObject : IFramedLifetime<FrameObject>
    {
        private IHostScreen? _hostScreen;
        private ObjectLayer? _layer;
        private readonly SubscriptionBag _subscriptions = new SubscriptionBag();
        private AsyncEventSource<FrameObject> _activating;
        private AsyncEventSource<FrameObject> _terminating;
        private EventSource<FrameObject> _update;
        private EventSource<FrameObject> _lateUpdate;
        private EventSource<FrameObject> _earlyUpdate;
        private EventSource<FrameObject> _alive;
        private EventSource<FrameObject> _dead;
        private string? _name;
        private LifeState _state = LifeState.New;
        private bool _isFrozen;
        private readonly FrameObjectInstanceType _instanceType = FrameObjectInstanceType.FrameObject;

        public AsyncEvent<FrameObject> Activating => _activating.Event;

        public AsyncEvent<FrameObject> Terminating => _terminating.Event;

        public Event<FrameObject> Alive => _alive.Event;

        public Event<FrameObject> Dead => _dead.Event;

        public Event<FrameObject> EarlyUpdate => _earlyUpdate.Event;

        public Event<FrameObject> LateUpdate => _lateUpdate.Event;

        public Event<FrameObject> Update => _update.Event;

        public SubscriptionRegister Subscriptions => _subscriptions.Register;

        public string? Name { get => _name; set => _name = value; }

        /// <summary>Get life state of <see cref="FrameObject"/></summary>
        public LifeState LifeState => _state;

        /// <summary>Get or set whether the <see cref="FrameObject"/> skips early updating, updating, and late updating. (Skips if true)</summary>
        public bool IsFrozen { get => _isFrozen; set => _isFrozen = value; }

        public IHostScreen? Screen => _hostScreen;
        public ObjectLayer? Layer => _layer;

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
        public bool TryGetLayer([MaybeNullWhen(false)] out ObjectLayer layer)
        {
            layer = _layer;
            return layer is not null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ObjectLayer GetValidLayer()
        {
            var layer = _layer;
            if(layer is null) {
                ThrowNoLayer();
                [DoesNotReturn] static void ThrowNoLayer() => throw new InvalidOperationException("No layer");
            }
            return layer;
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
        internal void InvokeEarlyUpdate() => _earlyUpdate.Invoke(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokeUpdate() => _update.Invoke(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokeLateUpdate() => _lateUpdate.Invoke(this);

        internal UniTask ActivateOnLayer(ObjectLayer layer, FrameTimingPoint timingPoint, CancellationToken ct)
        {
            var screen = layer.Screen;
            CheckStateAndThreadForActivation(screen, layer, timingPoint);
            ct.ThrowIfCancellationRequested();
            return ActivateOnLayerWithoutCheck(layer, timingPoint, screen, ct);
        }

        internal void CheckStateAndThreadForActivation([NotNull] IHostScreen? screen, [NotNull] ObjectLayer? layer, [NotNull] FrameTimingPoint? timingPoint)
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
            if(_state > LifeState.New) {
                throw new InvalidOperationException($"Cannot activate the {nameof(FrameObject)} twice.");
            }
        }

        internal async UniTask ActivateOnLayerWithoutCheck(ObjectLayer layer, FrameTimingPoint timingPoint, IHostScreen screen, CancellationToken ct)
        {
            Debug.Assert(_state == LifeState.New);
            Debug.Assert(_hostScreen is null);
            _hostScreen = screen;
            _layer = layer;
            _state = LifeState.Activating;
            ExceptionDispatchInfo? edi = null;
            try {
                await _activating.Invoke(this, ct);
                _activating.Clear();
            }
            catch(Exception ex) {
                if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) {
                    edi = ExceptionDispatchInfo.Capture(ex);
                }
            }

            // TODO: Consider the case that screen is already dead.
            if(screen.CurrentTiming.IsOutOfFrameLoop()) {
                await screen.Timings.FrameInitializing.Next(ct);
            }
            layer.AddFrameObject(this);
            await timingPoint.NextFrame(ct);
            Debug.Assert(_state >= LifeState.Alive);
            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) {
                edi?.Throw();
            }
            return;
        }

        internal async UniTask TerminateFromLayerWithoutCheck(ObjectLayer layer, FrameTimingPoint timingPoint, IHostScreen screen)
        {
            _state = LifeState.Terminating;
            try {
                await _terminating.Invoke(this, CancellationToken.None);
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
            if(_state >= LifeState.Terminating) { ThrowTerminateTwice(); }

            timingPoint ??= screen.Timings.Update;
            Debug.Assert(_state == LifeState.Alive);
            Debug.Assert(_layer is not null);

            if(IsPositionable(out var positionable) && positionable.HasChild) {
                var children = positionable.Children.AsSpan();
                using var tasks = new ParallelOperation();
                for(int i = children.Length - 1; i >= 0; i--) {
                    tasks.Add(children[i].TerminateFromLayer(timingPoint));
                }
                tasks.Add(TerminateFromLayerWithoutCheck(_layer, timingPoint, screen));
                return tasks.WhenAll();
            }
            else {
                return TerminateFromLayerWithoutCheck(_layer, timingPoint, screen);
            }

            [DoesNotReturn] static void ThrowNotActivated() => throw new InvalidOperationException($"Cannot terminate {nameof(FrameObject)} because it is not activated.");
            [DoesNotReturn] static void ThrowTerminateTwice() => throw new InvalidOperationException($"Cannot terminate {nameof(FrameObject)} twice.");
        }

        protected virtual void OnAlive() => _alive.Invoke(this);

        protected virtual void OnDead() => _dead.Invoke(this);

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
            if(IsPositionable(out var positionable)) {
                var parent = positionable.Parent;
                if(parent is not null) {
                    parent.Children.Remove(positionable);
                }

                // Clear children for returning inner pooled buffer though there are already no children.
                Debug.Assert(positionable.ChildrenCore.Count == 0);
                positionable.ChildrenCore.Clear();
                Debug.Assert(positionable.ChildrenCore.Capacity == 0);
            }
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
        public static async UniTask<T> Activate<T>(this T source, ObjectLayer layer, CancellationToken cancellationToken = default)
            where T : FrameObject
        {
            if(layer.TryGetScreen(out var screen) == false) {
                ThrowInvalidLayer();
            }
            await source.ActivateOnLayer(layer, screen.Timings.Update, cancellationToken);
            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask<T> Activate<T>(this T frameObject, ObjectLayer layer, FrameTimingPoint timingPoint, CancellationToken cancellationToken = default)
            where T : FrameObject
        {
            await frameObject.ActivateOnLayer(layer, timingPoint, cancellationToken);
            return frameObject;
        }

        public static async UniTask<T> Activate<T>(this T frameObject, ObjectLayer layer, FrameTiming timing, CancellationToken cancellationToken = default)
            where T : FrameObject
        {
            if(layer.TryGetScreen(out var screen) == false) {
                ThrowInvalidLayer();
            }
            var timingPoint = screen.Timings.GetTiming(timing);
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
            var timingPoint = frameObject.TryGetScreen(out var screen) ? screen.Timings.GetTiming(timing) : null;
            await frameObject.TerminateFromLayer(timingPoint);
            return frameObject;
        }

        [DoesNotReturn]
        private static void ThrowInvalidLayer() => throw new ArgumentException($"The layer is not associated with {nameof(IHostScreen)}");

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);
    }
}
