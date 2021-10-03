#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public static class CoroutineExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask StartCoroutine<TParent>(this TParent parent, Func<CoroutineState, TParent, UniTask> coroutine, FrameTiming timing = FrameTiming.Update)
            where TParent : FrameObject
        {
            return Coroutine.Start(parent, parent, coroutine, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask StartCoroutine(this IHostScreen parent, Func<CoroutineState, IHostScreen, UniTask> coroutine, FrameTiming timing = FrameTiming.Update)
        {
            return Coroutine.Start(parent, parent, coroutine, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask StartCoroutine<TParent, TState>(this TParent parent, TState state, Func<CoroutineState, TState, UniTask> coroutine, FrameTiming timing = FrameTiming.Update)
            where TParent : FrameObject
        {
            return Coroutine.Start(parent, state, coroutine, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask StartCoroutine<TState>(this IHostScreen parent, TState state, Func<CoroutineState, TState, UniTask> coroutine, FrameTiming timing = FrameTiming.Update)
        {
            return Coroutine.Start(parent, state, coroutine, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserveCoroutine<TParent>(this TParent parent, Func<CoroutineState, TParent, UniTask> coroutine, FrameTiming timing = FrameTiming.Update)
            where TParent : FrameObject
        {
            Coroutine.StartOrReserve(parent, parent, coroutine, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserveCoroutine(this IHostScreen parent, Func<CoroutineState, IHostScreen, UniTask> coroutine, FrameTiming timing = FrameTiming.Update)
        {
            Coroutine.StartOrReserve(parent, parent, coroutine, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserveCoroutine<TParent, TState>(this TParent parent, TState state, Func<CoroutineState, TState, UniTask> coroutine, FrameTiming timing = FrameTiming.Update)
            where TParent : FrameObject
        {
            Coroutine.StartOrReserve(parent, state, coroutine, timing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartOrReserveCoroutine<TState>(this IHostScreen parent, TState state, Func<CoroutineState, TState, UniTask> coroutine, FrameTiming timing = FrameTiming.Update)
        {
            Coroutine.StartOrReserve(parent, state, coroutine, timing);
        }
    }
}
