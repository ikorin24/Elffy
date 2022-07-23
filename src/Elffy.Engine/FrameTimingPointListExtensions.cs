#nullable enable
using System;
using System.Threading;

namespace Elffy
{
    public static class FrameTimingPointListExtensions
    {
        public static void OnEarlyUpdate(this FrameTimingPointList timings, Action action, CancellationToken ct = default)
            => OnTiming(timings.EarlyUpdate, action, ct);

        public static void OnEarlyUpdate(this FrameTimingPointList timings, Func<bool> func, CancellationToken ct = default)
            => OnTiming(timings.EarlyUpdate, func, ct);

        public static void OnUpdate(this FrameTimingPointList timings, Action action, CancellationToken ct = default)
            => OnTiming(timings.Update, action, ct);

        public static void OnUpdate(this FrameTimingPointList timings, Func<bool> func, CancellationToken ct = default)
            => OnTiming(timings.Update, func, ct);

        public static void OnLateUpdate(this FrameTimingPointList timings, Func<bool> func, CancellationToken ct = default)
            => OnTiming(timings.LateUpdate, func, ct);

        public static void OnLateUpdate(this FrameTimingPointList timings, Action action, CancellationToken ct = default)
            => OnTiming(timings.LateUpdate, action, ct);

        public static void OnFrameInitializing(this FrameTimingPointList timings, Action action, CancellationToken ct = default)
            => OnTiming(timings.FrameInitializing, action, ct);

        public static void OnFrameInitializing(this FrameTimingPointList timings, Func<bool> func, CancellationToken ct = default)
            => OnTiming(timings.FrameInitializing, func, ct);

        public static void OnAfterRendering(this FrameTimingPointList timings, Action action, CancellationToken ct = default)
            => OnTiming(timings.AfterRendering, action, ct);

        public static void OnAfterRendering(this FrameTimingPointList timings, Func<bool> func, CancellationToken ct = default)
            => OnTiming(timings.AfterRendering, func, ct);

        public static void OnBeforeRendering(this FrameTimingPointList timings, Action action, CancellationToken ct = default)
            => OnTiming(timings.BeforeRendering, action, ct);

        public static void OnBeforeRendering(this FrameTimingPointList timings, Func<bool> func, CancellationToken ct = default)
            => OnTiming(timings.BeforeRendering, func, ct);


        private static async void OnTiming(FrameTimingPoint tp, Action action, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(action);
            try {
                while(true) {
                    await tp.Next(ct);
                    action.Invoke();
                }
            }
            catch {
                if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                // Don't throw, ignore exceptions in user code.
            }
        }

        private static async void OnTiming(FrameTimingPoint tp, Func<bool> func, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(func);
            try {
                while(true) {
                    await tp.Next(ct);
                    if(func.Invoke() == false) {
                        return;
                    }
                }
            }
            catch {
                if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                // Don't throw, ignore exceptions in user code.
            }
        }
    }
}
