using Elffy.Core;
using Elffy.Exceptions;
using System;

namespace Elffy.Framing
{
    #region class FrameProcess
    /// <summary>一連のフレームプロセスの流れを表すオブジェクト</summary>
    public sealed class FrameProcess
    {
        /// <summary><see cref="ProcessObj"/> を有効にするレイヤー</summary>
        /// <remarks>
        /// ※ もしゲーム用途以外でこの <see cref="FrameProcess"/> を使いたい場合、
        /// その <see cref="IScreenHost"/> での <see cref="IScreenHost.Layers"/> の <see cref="LayerCollection.SystemLayer"/> を使うように
        /// 実装を変える必要がある
        /// </remarks>
        internal static readonly LayerBase TARGET_LAYER = Game.Layers.SystemLayer;

        /// <summary>何もしない動作を表す <see cref="FrameProcessBehavior"/> オブジェクト</summary>
        internal static readonly FrameProcessBehavior WAIT_BEHAVIOR = info => { };
        /// <summary>常にtrueを返す条件</summary>
        internal static readonly Func<bool> ALWAYS_TRUE = () => true;

        /// <summary>この <see cref="FrameProcess"/> の実体</summary>
        internal FrameProcessObject ProcessObj { get; } = new FrameProcessObject();

        /// <summary>コンストラクタ</summary>
        private FrameProcess() { }

        #region public Method
        /// <summary>指定した時間だけ、指定した処理を毎フレーム実行します</summary>
        /// <param name="time">実行時間</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess Begin(TimeSpan time, FrameProcessBehavior behavior)
        {
            ArgumentChecker.ThrowIf(time < TimeSpan.Zero, new ArgumentException($"{nameof(time)} is negative."));
            ArgumentChecker.ThrowIfNullArg(behavior, nameof(behavior));
            var frameProcess = new FrameProcess();
            frameProcess.ProcessObj.Activate(TARGET_LAYER);
            frameProcess.ProcessObj.AddBehavior(time, behavior);
            return frameProcess;
        }

        /// <summary>指定した処理を次のフレームに実行します</summary>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess Do(FrameProcessBehavior behavior)
        {
            ArgumentChecker.ThrowIfNullArg(behavior, nameof(behavior));
            var frameProcess = new FrameProcess();
            frameProcess.ProcessObj.Activate(TARGET_LAYER);
            frameProcess.ProcessObj.AddBehavior(behavior);
            return frameProcess;
        }

        /// <summary>指定した時間、フレームプロセスを停止します</summary>
        /// <param name="time">停止時間</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess Wait(TimeSpan time)
        {
            ArgumentChecker.ThrowIf(time < TimeSpan.Zero, new ArgumentException($"{nameof(time)} is negative."));
            var frameProcess = new FrameProcess();
            frameProcess.ProcessObj.Activate(TARGET_LAYER);
            frameProcess.ProcessObj.AddBehavior(time, WAIT_BEHAVIOR);
            return frameProcess;
        }

        /// <summary>指定した条件を満たす間、指定した処理を毎フレーム実行します</summary>
        /// <param name="condition">処理の継続条件</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess While(Func<bool> condition, FrameProcessBehavior behavior)
        {
            ArgumentChecker.ThrowIfNullArg(condition, nameof(condition));
            ArgumentChecker.ThrowIfNullArg(behavior, nameof(behavior));
            var frameProcess = new FrameProcess();
            frameProcess.ProcessObj.Activate(TARGET_LAYER);
            frameProcess.ProcessObj.AddBehavior(condition, behavior);
            return frameProcess;
        }

        /// <summary>指定した処理を毎フレーム実行し続けます</summary>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess WhileTrue(FrameProcessBehavior behavior)
        {
            ArgumentChecker.ThrowIfNullArg(behavior, nameof(behavior));
            var frameProcess = new FrameProcess();
            frameProcess.ProcessObj.Activate(TARGET_LAYER);
            frameProcess.ProcessObj.AddBehavior(ALWAYS_TRUE, behavior);
            return frameProcess;
        }
        #endregion public Method
    }
    #endregion

    #region class FrameProcessExtension
    public static class FrameProcessExtension
    {
        /// <summary>指定した時間だけ、指定した処理を毎フレーム実行します</summary>
        /// <param name="frameProcess">動作を追加するフレームプロセス</param>
        /// <param name="time">実行時間</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess Begin(this FrameProcess frameProcess, TimeSpan time, FrameProcessBehavior behavior)
        {
            ArgumentChecker.ThrowIf(time < TimeSpan.Zero, new ArgumentException($"{nameof(time)} is negative."));
            ArgumentChecker.ThrowIfNullArg(behavior, nameof(behavior));
            if(!frameProcess.ProcessObj.IsActivated) { frameProcess.ProcessObj.Activate(FrameProcess.TARGET_LAYER); }
            frameProcess.ProcessObj.AddBehavior(time, behavior);
            return frameProcess;
        }

        /// <summary>指定した処理を次のフレームに実行します</summary>
        /// <param name="frameProcess">動作を追加するフレームプロセス</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess Do(this FrameProcess frameProcess, FrameProcessBehavior behavior)
        {
            ArgumentChecker.ThrowIfNullArg(behavior, nameof(behavior));
            if(!frameProcess.ProcessObj.IsActivated) { frameProcess.ProcessObj.Activate(FrameProcess.TARGET_LAYER); }
            frameProcess.ProcessObj.AddBehavior(behavior);
            return frameProcess;
        }

        /// <summary>指定した時間、フレームプロセスを停止します</summary>
        /// <param name="frameProcess">動作を追加するフレームプロセス</param>
        /// <param name="time">停止時間</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess Wait(this FrameProcess frameProcess, TimeSpan time)
        {
            ArgumentChecker.ThrowIf(time < TimeSpan.Zero, new ArgumentException($"{nameof(time)} is negative."));
            if(!frameProcess.ProcessObj.IsActivated) { frameProcess.ProcessObj.Activate(FrameProcess.TARGET_LAYER); }
            frameProcess.ProcessObj.AddBehavior(time, FrameProcess.WAIT_BEHAVIOR);
            return frameProcess;
        }

        /// <summary>指定した条件を満たす間、指定した処理を毎フレーム実行します</summary>
        /// <param name="frameProcess">動作を追加するフレームプロセス</param>
        /// <param name="condition">処理の継続条件</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess While(this FrameProcess frameProcess, Func<bool> condition, FrameProcessBehavior behavior)
        {
            ArgumentChecker.ThrowIfNullArg(condition, nameof(condition));
            ArgumentChecker.ThrowIfNullArg(behavior, nameof(behavior));
            if(!frameProcess.ProcessObj.IsActivated) { frameProcess.ProcessObj.Activate(FrameProcess.TARGET_LAYER); }
            frameProcess.ProcessObj.AddBehavior(condition, behavior);
            return frameProcess;
        }

        /// <summary>指定した処理を毎フレーム実行し続けます</summary>
        /// <param name="frameProcess">動作を追加するフレームプロセス</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess WhileTrue(this FrameProcess frameProcess, FrameProcessBehavior behavior)
        {
            ArgumentChecker.ThrowIfNullArg(behavior, nameof(behavior));
            if(!frameProcess.ProcessObj.IsActivated) { frameProcess.ProcessObj.Activate(FrameProcess.TARGET_LAYER); }
            frameProcess.ProcessObj.AddBehavior(FrameProcess.ALWAYS_TRUE, behavior);
            return frameProcess;
        }

        /// <summary>このフレームプロセスの以降の動作をすべてキャンセルし停止させます</summary>
        public static void Cancel(this FrameProcess frameProcess) => frameProcess.ProcessObj.Cancel();
    }
    #endregion
}
