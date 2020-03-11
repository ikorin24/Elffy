#nullable enable
using Elffy.Core;
using Elffy.Effective.Internal;
using Elffy.Exceptions;
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Framing
{
    /// <summary>一連のフレームプロセスの流れを表すオブジェクト</summary>
    public readonly struct FrameProcess
    {
        /// <summary><see cref="ProcessObj"/> を有効にするレイヤー</summary>
        internal static SystemLayer TARGET_LAYER => Engine.CurrentScreen.Layers.SystemLayer;

        /// <summary>何もしない動作を表す <see cref="FrameProcessBehavior"/> オブジェクト</summary>
        internal static readonly FrameProcessBehavior WAIT_BEHAVIOR = info => { };
        /// <summary>常にtrueを返す条件</summary>
        internal static readonly Func<bool> ALWAYS_TRUE = () => true;

        #pragma warning disable CS0649
        private readonly FrameProcessObject? _processObj;
        #pragma warning restore CS0649

        /// <summary>この <see cref="FrameProcess"/> の実体</summary>
        internal readonly FrameProcessObject ProcessObj => _processObj ?? UnsafeTool.SetValue(_processObj!, new FrameProcessObject());

        /// <summary>指定した時間だけ、指定した処理を毎フレーム実行します</summary>
        /// <param name="time">実行時間</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess Begin(TimeSpan time, FrameProcessBehavior behavior) => new FrameProcess().Begin(time, behavior);

        /// <summary>指定した処理を次のフレームに実行します</summary>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess Do(FrameProcessBehavior behavior) => new FrameProcess().Do(behavior);

        /// <summary>指定した時間、フレームプロセスを停止します</summary>
        /// <param name="time">停止時間</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess Wait(TimeSpan time) => new FrameProcess().Wait(time);

        /// <summary>指定した条件を満たす間、指定した処理を毎フレーム実行します</summary>
        /// <param name="condition">処理の継続条件</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess While(Func<bool> condition, FrameProcessBehavior behavior) => new FrameProcess().While(condition, behavior);

        /// <summary>指定した処理を毎フレーム実行し続けます</summary>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess WhileTrue(FrameProcessBehavior behavior) => new FrameProcess().WhileTrue(behavior);
    }

    public static class FrameProcessExtension
    {
        /// <summary>指定した時間だけ、指定した処理を毎フレーム実行します</summary>
        /// <param name="frameProcess">動作を追加するフレームプロセス</param>
        /// <param name="time">実行時間</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameProcess Begin(this FrameProcess frameProcess, TimeSpan time, FrameProcessBehavior behavior)
        {
            ArgumentChecker.ThrowArgumentIf(time < TimeSpan.Zero, $"{nameof(time)} is negative.");
            ArgumentChecker.ThrowIfNullArg(behavior, nameof(behavior));

            var obj = frameProcess.ProcessObj;
            obj.Activate(FrameProcess.TARGET_LAYER);
            obj.AddLifeSpanBehavior(time, behavior);
            return frameProcess;
        }

        /// <summary>指定した処理を次のフレームに実行します</summary>
        /// <param name="frameProcess">動作を追加するフレームプロセス</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameProcess Do(this FrameProcess frameProcess, FrameProcessBehavior behavior)
        {
            ArgumentChecker.ThrowIfNullArg(behavior, nameof(behavior));

            var obj = frameProcess.ProcessObj;
            obj.Activate(FrameProcess.TARGET_LAYER);
            obj.AddFrameSpanBehavior(1, behavior);
            return frameProcess;
        }

        /// <summary>指定した時間、フレームプロセスを停止します</summary>
        /// <param name="frameProcess">動作を追加するフレームプロセス</param>
        /// <param name="time">停止時間</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameProcess Wait(this FrameProcess frameProcess, TimeSpan time)
        {
            ArgumentChecker.ThrowArgumentIf(time < TimeSpan.Zero, $"{nameof(time)} is negative.");

            var obj = frameProcess.ProcessObj;
            obj.Activate(FrameProcess.TARGET_LAYER);
            obj.AddLifeSpanBehavior(time, FrameProcess.WAIT_BEHAVIOR);
            return frameProcess;
        }

        /// <summary>指定した条件を満たす間、指定した処理を毎フレーム実行します</summary>
        /// <param name="frameProcess">動作を追加するフレームプロセス</param>
        /// <param name="condition">処理の継続条件</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameProcess While(this FrameProcess frameProcess, Func<bool> condition, FrameProcessBehavior behavior)
        {
            ArgumentChecker.ThrowIfNullArg(condition, nameof(condition));
            ArgumentChecker.ThrowIfNullArg(behavior, nameof(behavior));

            var obj = frameProcess.ProcessObj;
            obj.Activate(FrameProcess.TARGET_LAYER);
            obj.AddConditionalBehavior(condition, behavior);
            return frameProcess;
        }

        /// <summary>指定した処理を毎フレーム実行し続けます</summary>
        /// <param name="frameProcess">動作を追加するフレームプロセス</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameProcess WhileTrue(this FrameProcess frameProcess, FrameProcessBehavior behavior)
        {
            ArgumentChecker.ThrowIfNullArg(behavior, nameof(behavior));

            var obj = frameProcess.ProcessObj;
            obj.Activate(FrameProcess.TARGET_LAYER);
            obj.AddConditionalBehavior(FrameProcess.ALWAYS_TRUE, behavior);
            return frameProcess;
        }

        /// <summary>このフレームプロセスの以降の動作をすべてキャンセルし停止させます</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Cancel(this FrameProcess frameProcess) => frameProcess.ProcessObj.Cancel();
    }
}
