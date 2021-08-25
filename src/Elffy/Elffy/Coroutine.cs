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
    // 3. It is not possible to wait for a coroutine to finish.
    // 4. Not thread-safe (Don't call 'Coroutine.CreateXXX' and 'FrameObject.Activate' at the same time in parallel.)

    /// <summary>Privides a coroutine attached to a parent object.</summary>
    public static class Coroutine
    {
        /// <summary>Create a coroutine of the specified <see cref="FrameObject"/></summary>
        /// <param name="parent">the parent of the coroutine</param>
        /// <param name="coroutine">the coroutine function</param>
        /// <param name="timing">the timing when the coroutine starts</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Create(FrameObject parent, Func<CoroutineState, UniTask> coroutine, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(parent is null) { ThrowNullArg(nameof(parent)); }
            if(coroutine is null) { ThrowNullArg(nameof(coroutine)); }
            timing.ThrowArgExceptionIfNotSpecified();

            if(parent.LifeState.IsBefore(LifeState.Activated)) {
                LazyStart(parent, DummyState.Null, coroutine, null, timing);
            }
            else {
                StartCoroutine(new CoroutineState(parent), DummyState.Null, coroutine, null, timing);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Create(IHostScreen parent, Func<CoroutineState, UniTask> coroutine, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(parent is null) { ThrowNullArg(nameof(parent)); }
            if(coroutine is null) { ThrowNullArg(nameof(coroutine)); }
            timing.ThrowArgExceptionIfNotSpecified();
            // TODO: if screen is not managed by the engine ...
            StartCoroutine(new CoroutineState(parent), DummyState.Null, coroutine, null, timing);
        }

        /// <summary>Create a coroutine of the specified <see cref="FrameObject"/> with a state</summary>
        /// <typeparam name="T">type of <paramref name="state"/></typeparam>
        /// <param name="parent">the parent of the coroutine</param>
        /// <param name="state">state of the coroutine</param>
        /// <param name="coroutine">the coroutine function</param>
        /// <param name="timing">the timing when the coroutine starts</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Create<T>(FrameObject parent, T state, Func<CoroutineState, T, UniTask> coroutine, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(parent is null) { ThrowNullArg(nameof(parent)); }
            if(coroutine is null) { ThrowNullArg(nameof(coroutine)); }
            timing.ThrowArgExceptionIfNotSpecified();

            if(parent.LifeState.IsBefore(LifeState.Activated)) {
                LazyStart(parent, state, coroutine, null, timing);
            }
            else {
                StartCoroutine(new CoroutineState(parent), state, coroutine, null, timing);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Create<T>(IHostScreen parent, T state, Func<CoroutineState, T, UniTask> coroutine, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(parent is null) { ThrowNullArg(nameof(parent)); }
            if(coroutine is null) { ThrowNullArg(nameof(coroutine)); }
            timing.ThrowArgExceptionIfNotSpecified();
            // TODO: if screen is not managed by the engine ...
            StartCoroutine(new CoroutineState(parent), state, coroutine, null, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateWithCatch(FrameObject parent, Func<CoroutineState, UniTask> coroutine, Action<Exception> onCatch, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(parent is null) { ThrowNullArg(nameof(parent)); }
            if(coroutine is null) { ThrowNullArg(nameof(coroutine)); }
            timing.ThrowArgExceptionIfNotSpecified();

            if(parent.LifeState.IsBefore(LifeState.Activated)) {
                LazyStart(parent, DummyState.Null, coroutine, onCatch, timing);
            }
            else {
                StartCoroutine(new CoroutineState(parent), DummyState.Null, coroutine, onCatch, timing);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateWithCatch(IHostScreen parent, Func<CoroutineState, UniTask> coroutine, Action<Exception> onCatch, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(parent is null) { ThrowNullArg(nameof(parent)); }
            if(coroutine is null) { ThrowNullArg(nameof(coroutine)); }
            timing.ThrowArgExceptionIfNotSpecified();
            // TODO: if screen is not managed by the engine ...
            StartCoroutine(new CoroutineState(parent), DummyState.Null, coroutine, onCatch, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateWithCatch<T>(FrameObject parent, T state, Func<CoroutineState, T, UniTask> coroutine, Action<Exception> onCatch, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(parent is null) { ThrowNullArg(nameof(parent)); }
            if(coroutine is null) { ThrowNullArg(nameof(coroutine)); }
            timing.ThrowArgExceptionIfNotSpecified();

            if(parent.LifeState.IsBefore(LifeState.Activated)) {
                LazyStart(parent, state, coroutine, onCatch, timing);
            }
            else {
                StartCoroutine(new CoroutineState(parent), state, coroutine, onCatch, timing);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateWithCatch<T>(IHostScreen parent, T state, Func<CoroutineState, T, UniTask> coroutine, Action<Exception> onCatch, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(parent is null) { ThrowNullArg(nameof(parent)); }
            if(coroutine is null) { ThrowNullArg(nameof(coroutine)); }
            timing.ThrowArgExceptionIfNotSpecified();
            // TODO: if screen is not managed by the engine ...
            StartCoroutine(new CoroutineState(parent), state, coroutine, onCatch, timing);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void LazyStart<T>(FrameObject parent, T state, Delegate coroutine, Action<Exception>? onCatch, FrameLoopTiming timing)
        {
            if(typeof(T) == typeof(DummyState)) {
                if(onCatch is null) {
                    // [capture] coroutine, timing
                    parent.Activated += f => StartCoroutine(new CoroutineState(f), DummyState.Null, coroutine, null, timing);
                }
                else {
                    // [capture] coroutine, onCatch, timing
                    parent.Activated += f => StartCoroutine(new CoroutineState(f), DummyState.Null, coroutine, onCatch, timing);
                }
            }
            else {
                if(onCatch is null) {
                    // [capture] state, coroutine, timing
                    parent.Activated += f => StartCoroutine(new CoroutineState(f), state, coroutine, null, timing);
                }
                else {
                    // [capture] state, coroutine, onCatch, timing
                    parent.Activated += f => StartCoroutine(new CoroutineState(f), state, coroutine, onCatch, timing);
                }
            }
        }

        private static async void StartCoroutine<T>(CoroutineState coroutineState, T state, Delegate coroutine, Action<Exception>? onCatch, FrameLoopTiming timing)
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
                await coroutineState.Screen.AsyncBack.ToTiming(timing);
                if(typeof(T) == typeof(DummyState)) {
                    var noStateCoroutine = SafeCast.As<Func<CoroutineState, UniTask>>(coroutine);
                    await noStateCoroutine(coroutineState);
                }
                else {
                    var statefulCoroutine = SafeCast.As<Func<CoroutineState, T, UniTask>>(coroutine);
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
    }
}
