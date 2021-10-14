#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.UI;
using Elffy.Threading;

namespace Elffy
{
    /// <summary>The base class which is controlled by the engine. It provides frame update operations and operations to be managed by the engine.</summary>
    public abstract class FrameObject
    {
        private IHostScreen? _hostScreen;
        private Layer? _layer;
        private LifeState _state = LifeState.New;
        private bool _isFrozen;

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

        internal async UniTask ActivateOnWorldLayer(WorldLayer layer, FrameTimingPoint timingPoint, CancellationToken cancellationToken)
        {
            if(layer is null) { ThrowNullArg(); }
            if(timingPoint is null) { ThrowNullArg(); }
            if(layer.TryGetHostScreen(out var screen) == false) { ThrowInvalidLayer(); }
            if(Engine.CurrentContext != screen) { ThrowContextMismatch(); }
            cancellationToken.ThrowIfCancellationRequested();

            if(_state == LifeState.Activating) {
                ThrowNowActivating();
            }
            if(_state.IsAfter(LifeState.New)) {
                await timingPoint.NextOrNow(cancellationToken);
                return;
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
                    await screen.TimingPoints.Update.NextOrNow(CancellationToken.None);
                    try {
                        await TerminateFromLayer(timingPoint);
                    }
                    catch {
                        // Ignore exceptions in Terminate()
                    }
                }

                throw;  // Throw exceptions of activating.
            }
            if(screen.CurrentTiming.IsOutOfFrameLoop()) {
                await screen.TimingPoints.FrameInitializing.Next(cancellationToken);
            }
            layer.AddFrameObject(this);
            await WaitForNextFrame(screen, timingPoint, cancellationToken);
            Debug.Assert(_state.IsSameOrAfter(LifeState.Alive));
            return;

            [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(layer));
            [DoesNotReturn] static void ThrowInvalidLayer() => throw new ArgumentException($"{nameof(layer)} is not associated with {nameof(IHostScreen)}");
            [DoesNotReturn] static void ThrowContextMismatch() => throw new InvalidOperationException("Invalid current context.");
            [DoesNotReturn] static void ThrowNowActivating() => throw new InvalidOperationException($"Cannot call Activate method when the life state is {LifeState.Activating}.");
        }

        private static async UniTask WaitForNextFrame(IHostScreen screen, FrameTimingPoint timingPoint, CancellationToken cancellationToken)
        {
            await screen.TimingPoints.FrameInitializing.Next(cancellationToken);
            await timingPoint.NextOrNow(cancellationToken);
        }

        internal void ActivateOnUILayer(UILayer layer)
        {
            if(_state != LifeState.New) { return; }
            Debug.Assert(layer is not null);
            Debug.Assert(layer.LifeState == LayerLifeState.Alive);
            Debug.Assert(GetType() == typeof(UIRenderable));

            var screen = layer.Screen;
            Debug.Assert(screen is not null);
            Debug.Assert(Engine.CurrentContext == screen);

            _hostScreen = screen;
            _state = LifeState.Activating;
            _layer = layer;

            // [NOTE] UIRenderable must complete OnActivating synchronously. (See the implementation of UIRenderable)
            OnActivating(default).SyncGetResult();

            layer.AddFrameObject(this);
            //await WaitForNextFrame(screen, timingPoint, cancellationToken);   // TODO: Lazy applying control list, Wait for alive
            //Debug.Assert(_state.IsSameOrAfter(LifeState.Alive));  // TODO:
            return;
        }

        internal async UniTask TerminateFromLayer(FrameTimingPoint? timingPoint)
        {
            var context = Engine.CurrentContext;
            if(context is null || context != _hostScreen) { ThrowContextMismatch(); }

            Debug.Assert(timingPoint is not null || _state == LifeState.New || _state.IsSameOrAfter(LifeState.Terminating));

            if(_state == LifeState.New) { ThrowNotActivated(); }
            if(_state.IsSameOrAfter(LifeState.Terminating)) { ThrowTerminateTwice(); }

            await OnTerminating();

            Debug.Assert(_state == LifeState.Activating || _state == LifeState.Alive);
            Debug.Assert(_layer is not null);
            _state = LifeState.Terminating;
            _layer.RemoveFrameObject(this);
            var screen = _hostScreen;
            Debug.Assert(screen is not null);
            timingPoint ??= screen.TimingPoints.Update;
            await WaitForNextFrame(screen, timingPoint, CancellationToken.None);
            Debug.Assert(_state == LifeState.Dead);
            return;

            [DoesNotReturn] static void ThrowContextMismatch() => throw new InvalidOperationException("Invalid current context.");
            [DoesNotReturn] static void ThrowNotActivated() => throw new InvalidOperationException($"Cannot terminate {nameof(FrameObject)} because it is not activated.");
            [DoesNotReturn] static void ThrowTerminateTwice() => throw new InvalidOperationException($"Cannot terminate {nameof(FrameObject)} twice.");
        }

        protected virtual void OnEarlyUpdate() => EarlyUpdated?.Invoke(this);

        protected virtual void OnUpdate() => Updated?.Invoke(this);

        protected virtual void OnLateUpdte() => LateUpdated?.Invoke(this);

        protected virtual UniTask<AsyncUnit> OnActivating(CancellationToken cancellationToken) => UniTask.FromResult(AsyncUnit.Default);

        protected virtual UniTask OnTerminating() => UniTask.CompletedTask;

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
    }

    public static class FrameObjectActivationExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask<T> Activate<T>(this T source, WorldLayer layer, CancellationToken cancellationToken = default)
            where T : FrameObject
        {
            if(layer.TryGetHostScreen(out var screen) == false) {
                ThrowInvalidLayer();
            }
            await source.ActivateOnWorldLayer(layer, screen.TimingPoints.Update, cancellationToken);
            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask<T> Activate<T>(this T frameObject, WorldLayer layer, FrameTimingPoint timingPoint, CancellationToken cancellationToken = default)
            where T : FrameObject
        {
            await frameObject.ActivateOnWorldLayer(layer, timingPoint, cancellationToken);
            return frameObject;
        }

        public static async UniTask<T> Activate<T>(this T frameObject, WorldLayer layer, FrameTiming timing, CancellationToken cancellationToken = default)
            where T : FrameObject
        {
            if(layer.TryGetHostScreen(out var screen) == false) {
                ThrowInvalidLayer();
            }
            var timingPoint = screen.TimingPoints.TimingOf(timing);
            await frameObject.ActivateOnWorldLayer(layer, timingPoint, cancellationToken);
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
            var timingPoint = frameObject.TryGetHostScreen(out var screen) ? screen.TimingPoints.TimingOf(timing) : null;
            await frameObject.TerminateFromLayer(timingPoint);
            return frameObject;
        }

        [DoesNotReturn]
        private static void ThrowInvalidLayer() => throw new ArgumentException($"The layer is not associated with {nameof(IHostScreen)}");

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);
    }
}
