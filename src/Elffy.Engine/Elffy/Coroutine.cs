#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    // [NOTE]
    // 1. A coroutine is started when the parent object becomes active. (The parent is FrameObject or IHostScreen)
    // 2. Coroutines can be executed only while the parent object is alive.
    // 3. Not thread-safe (Don't call 'Coroutine.StartXXX' and 'FrameObject.Activate' at the same time in parallel.)

    /// <summary>Privides a coroutine attached to a parent object.</summary>
    public static class Coroutine
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask Start(FrameObject parent, Func<CoroutineState, UniTask> coroutine)
        {
            return Start(parent, coroutine, FrameTiming.Update);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask Start(FrameObject parent, Func<CoroutineState, UniTask> coroutine, FrameTiming timing)
        {
            CheckArgs(parent, coroutine, timing);
            return StartPrivate(parent, DummyState.Null, coroutine, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask Start(IHostScreen parent, Func<CoroutineState, UniTask> coroutine)
        {
            return Start(parent, coroutine, FrameTiming.Update);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask Start(IHostScreen parent, Func<CoroutineState, UniTask> coroutine, FrameTiming timing)
        {
            CheckArgs(parent, coroutine, timing);
            return StartPrivate(parent, DummyState.Null, coroutine, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask Start<TState>(FrameObject parent, TState state, Func<CoroutineState, TState, UniTask> coroutine)
        {
            return Start(parent, state, coroutine, FrameTiming.Update);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask Start<TState>(FrameObject parent, TState state, Func<CoroutineState, TState, UniTask> coroutine, FrameTiming timing)
        {
            CheckArgs(parent, coroutine, timing);
            return StartPrivate(parent, state, coroutine, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask Start<TState>(IHostScreen parent, TState state, Func<CoroutineState, TState, UniTask> coroutine)
        {
            return Start(parent, state, coroutine, FrameTiming.Update);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask Start<TState>(IHostScreen parent, TState state, Func<CoroutineState, TState, UniTask> coroutine, FrameTiming timing)
        {
            CheckArgs(parent, coroutine, timing);
            return StartPrivate(parent, state, coroutine, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserve(FrameObject parent, Func<CoroutineState, UniTask> coroutine)
        {
            StartOrReserve(parent, coroutine, FrameTiming.Update);
        }

        /// <summary>Create a coroutine of the specified <see cref="FrameObject"/></summary>
        /// <param name="parent">the parent of the coroutine</param>
        /// <param name="coroutine">the coroutine function</param>
        /// <param name="timing">the timing when the coroutine starts</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserve(FrameObject parent, Func<CoroutineState, UniTask> coroutine, FrameTiming timing)
        {
            CheckArgs(parent, coroutine, timing);
            StartOrReservePrivate(parent, DummyState.Null, coroutine, null, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserve(IHostScreen parent, Func<CoroutineState, UniTask> coroutine)
        {
            StartOrReserve(parent, coroutine, FrameTiming.Update);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserve(IHostScreen parent, Func<CoroutineState, UniTask> coroutine, FrameTiming timing)
        {
            CheckArgs(parent, coroutine, timing);
            StartOrReservePrivate(parent, DummyState.Null, coroutine, null, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserve<TState>(FrameObject parent, TState state, Func<CoroutineState, TState, UniTask> coroutine)
        {
            StartOrReserve(parent, state, coroutine, FrameTiming.Update);
        }

        /// <summary>Create a coroutine of the specified <see cref="FrameObject"/> with a state</summary>
        /// <typeparam name="TState">type of <paramref name="state"/></typeparam>
        /// <param name="parent">the parent of the coroutine</param>
        /// <param name="state">state of the coroutine</param>
        /// <param name="coroutine">the coroutine function</param>
        /// <param name="timing">the timing when the coroutine starts</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserve<TState>(FrameObject parent, TState state, Func<CoroutineState, TState, UniTask> coroutine, FrameTiming timing)
        {
            CheckArgs(parent, coroutine, timing);
            StartOrReservePrivate(parent, state, coroutine, null, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserve<TState>(IHostScreen parent, TState state, Func<CoroutineState, TState, UniTask> coroutine)
        {
            StartOrReserve(parent, state, coroutine, FrameTiming.Update);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserve<TState>(IHostScreen parent, TState state, Func<CoroutineState, TState, UniTask> coroutine, FrameTiming timing)
        {
            CheckArgs(parent, coroutine, timing);
            StartOrReservePrivate(parent, state, coroutine, null, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserveWithCatch(FrameObject parent, Func<CoroutineState, UniTask> coroutine, Action<Exception> onCatch)
        {
            StartOrReserveWithCatch(parent, coroutine, onCatch, FrameTiming.Update);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserveWithCatch(FrameObject parent, Func<CoroutineState, UniTask> coroutine, Action<Exception> onCatch, FrameTiming timing)
        {
            CheckArgs(parent, coroutine, timing);
            StartOrReservePrivate(parent, DummyState.Null, coroutine, onCatch, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserveWithCatch(IHostScreen parent, Func<CoroutineState, UniTask> coroutine, Action<Exception> onCatch)
        {
            StartOrReserveWithCatch(parent, coroutine, onCatch, FrameTiming.Update);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserveWithCatch(IHostScreen parent, Func<CoroutineState, UniTask> coroutine, Action<Exception> onCatch, FrameTiming timing)
        {
            CheckArgs(parent, coroutine, timing);
            StartOrReservePrivate(parent, DummyState.Null, coroutine, onCatch, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserveWithCatch<TState>(FrameObject parent, TState state, Func<CoroutineState, TState, UniTask> coroutine, Action<Exception> onCatch)
        {
            StartOrReserveWithCatch(parent, state, coroutine, onCatch, FrameTiming.Update);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserveWithCatch<TState>(FrameObject parent, TState state, Func<CoroutineState, TState, UniTask> coroutine, Action<Exception> onCatch, FrameTiming timing)
        {
            CheckArgs(parent, coroutine, timing);
            StartOrReservePrivate(parent, state, coroutine, onCatch, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserveWithCatch<TState>(IHostScreen parent, TState state, Func<CoroutineState, TState, UniTask> coroutine, Action<Exception> onCatch)
        {
            StartOrReserveWithCatch(parent, state, coroutine, onCatch, FrameTiming.Update);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserveWithCatch<TState>(IHostScreen parent, TState state, Func<CoroutineState, TState, UniTask> coroutine, Action<Exception> onCatch, FrameTiming timing)
        {
            CheckArgs(parent, coroutine, timing);
            StartOrReservePrivate(parent, state, coroutine, onCatch, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckArgs(object parent, Delegate coroutine, FrameTiming timing)
        {
            Debug.Assert(parent is FrameObject || parent is IHostScreen || parent is null);
            if(parent is null) { ThrowNullArg(nameof(parent)); }
            if(coroutine is null) { ThrowNullArg(nameof(coroutine)); }
            timing.ThrowArgExceptionIfNotSpecified();
        }

        private static void StartOrReservePrivate<TParent, TState>(TParent parent, TState state, Delegate coroutine, Action<Exception>? onCatch, FrameTiming timing) where TParent : class
        {
            if(typeof(TParent) == typeof(IHostScreen)) {
                var parentScreen = SafeCast.As<IHostScreen>(parent);
                if(parentScreen.IsRunning == false) {
                    ReserveCoroutine(parentScreen, state, coroutine, onCatch, timing);
                }
                else {
                    StartCoroutine(new CoroutineState(parentScreen), state, coroutine, onCatch, timing).Forget();
                }
            }
            else if(typeof(TParent) == typeof(FrameObject)) {
                var parentFrameObject = SafeCast.As<FrameObject>(parent);
                if(parentFrameObject.LifeState.IsBefore(LifeState.Activated)) {
                    ReserveCoroutine(parentFrameObject, state, coroutine, null, timing);
                }
                else {
                    StartCoroutine(new CoroutineState(parentFrameObject), state, coroutine, null, timing).Forget();
                }
            }
            else {
                Debug.Fail("Something wrong");
            }
        }

        private static UniTask StartPrivate<TParent, TState>(TParent parent, TState state, Delegate coroutine, FrameTiming timing) where TParent : class
        {
            if(typeof(TParent) == typeof(IHostScreen)) {
                var parentScreen = SafeCast.As<IHostScreen>(parent);
                if(parentScreen.IsRunning == false) {
                    ThrowParentNotActivated();
                    return UniTask.CompletedTask;
                }
                else {
                    return StartCoroutine(new CoroutineState(parentScreen), state, coroutine, null, timing);
                }

            }
            else if(typeof(TParent) == typeof(FrameObject)) {
                var parentFrameObject = SafeCast.As<FrameObject>(parent);
                if(parentFrameObject.LifeState.IsBefore(LifeState.Activated)) {
                    ThrowParentNotActivated();
                    return UniTask.CompletedTask;
                }
                else {
                    return StartCoroutine(new CoroutineState(parentFrameObject), state, coroutine, null, timing);
                }
            }
            else {
                Debug.Fail("Something wrong");
                return UniTask.CompletedTask;
            }
        }

        private static void ReserveCoroutine<TParent, TState>(TParent parent, TState state, Delegate coroutine, Action<Exception>? onCatch, FrameTiming timing)
            where TParent : class
        {
            if(typeof(TParent) == typeof(IHostScreen)) {
                var parentScreen = SafeCast.As<IHostScreen>(parent);
                if(typeof(TState) == typeof(DummyState)) {
                    if(onCatch is null) {
                        // [capture] coroutine, timing
                        parentScreen.Initialized += p => StartCoroutine(new CoroutineState(p), DummyState.Null, coroutine, null, timing).Forget();
                    }
                    else {
                        // [capture] coroutine, onCatch, timing
                        parentScreen.Initialized += p => StartCoroutine(new CoroutineState(p), DummyState.Null, coroutine, onCatch, timing).Forget();
                    }
                    return;
                }
                else {
                    if(onCatch is null) {
                        // [capture] state, coroutine, timing
                        parentScreen.Initialized += p => StartCoroutine(new CoroutineState(p), state, coroutine, null, timing).Forget();
                    }
                    else {
                        // [capture] state, coroutine, onCatch, timing
                        parentScreen.Initialized += p => StartCoroutine(new CoroutineState(p), state, coroutine, onCatch, timing).Forget();
                    }
                    return;
                }
            }
            else if(typeof(TParent) == typeof(FrameObject)) {
                var parentFrameObject = SafeCast.As<FrameObject>(parent);
                if(typeof(TState) == typeof(DummyState)) {
                    if(onCatch is null) {
                        // [capture] coroutine, timing
                        parentFrameObject.Activated += f => StartCoroutine(new CoroutineState(f), DummyState.Null, coroutine, null, timing).Forget();
                    }
                    else {
                        // [capture] coroutine, onCatch, timing
                        parentFrameObject.Activated += f => StartCoroutine(new CoroutineState(f), DummyState.Null, coroutine, onCatch, timing).Forget();
                    }
                    return;
                }
                else {
                    if(onCatch is null) {
                        // [capture] state, coroutine, timing
                        parentFrameObject.Activated += f => StartCoroutine(new CoroutineState(f), state, coroutine, null, timing).Forget();
                    }
                    else {
                        // [capture] state, coroutine, onCatch, timing
                        parentFrameObject.Activated += f => StartCoroutine(new CoroutineState(f), state, coroutine, onCatch, timing).Forget();
                    }
                    return;
                }
            }
            else {
                Debug.Fail("Something wrong");
                return;
            }
        }

        private static async UniTask StartCoroutine<TState>(CoroutineState coroutineState, TState state, Delegate coroutine, Action<Exception>? onCatch, FrameTiming timing)
        {
            var fo = coroutineState.FrameObject;
            if(fo is null) {
                if(coroutineState.Screen.IsRunning == false) { return; }
            }
            else {
                Debug.Assert(fo.LifeState.IsSameOrAfter(LifeState.Activated));
                if(fo.LifeState == LifeState.Dead) { return; }
            }
            try {
                await coroutineState.TimingOf(timing).Switch();
                if(typeof(TState) == typeof(DummyState)) {
                    var noStateCoroutine = SafeCast.As<Func<CoroutineState, UniTask>>(coroutine);
                    await noStateCoroutine(coroutineState);
                }
                else {
                    var statefulCoroutine = SafeCast.As<Func<CoroutineState, TState, UniTask>>(coroutine);
                    await statefulCoroutine(coroutineState, state);
                }
            }
            catch(Exception ex) {
                onCatch?.Invoke(ex);
            }
        }

        private sealed class DummyState
        {
            public static DummyState? Null => null;
        }

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);

        [DoesNotReturn]
        public static void ThrowParentNotActivated() => throw new ArgumentException("The parent of the coroutine is not activated yet.");
    }
}
