using Elffy.Core;
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
        /// <param name="time">実行時間(ms)</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess Begin(int time, FrameProcessBehavior behavior)
        {
            if(time <= 0) { throw new ArgumentException($"{nameof(time)} must be bigger than 1."); }
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
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
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            var frameProcess = new FrameProcess();
            frameProcess.ProcessObj.Activate(TARGET_LAYER);
            frameProcess.ProcessObj.AddBehavior(behavior);
            return frameProcess;
        }

        /// <summary>指定した時間、フレームプロセスを停止します</summary>
        /// <param name="time">停止時間(ms)</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess Wait(int time)
        {
            if(time < 0) { throw new ArgumentException($"Time must be bigger than 0."); }
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
            if(condition == null) { throw new ArgumentNullException(nameof(condition)); }
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
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
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
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
        /// <param name="time">実行時間(ms)</param>
        /// <param name="behavior">実行する処理</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess Begin(this FrameProcess frameProcess, int time, FrameProcessBehavior behavior)
        {
            if(time <= 0) { throw new ArgumentException($"{nameof(time)} must be bigger than 1."); }
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
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
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            if(!frameProcess.ProcessObj.IsActivated) { frameProcess.ProcessObj.Activate(FrameProcess.TARGET_LAYER); }
            frameProcess.ProcessObj.AddBehavior(behavior);
            return frameProcess;
        }

        /// <summary>指定した時間、フレームプロセスを停止します</summary>
        /// <param name="frameProcess">動作を追加するフレームプロセス</param>
        /// <param name="time">停止時間(ms)</param>
        /// <returns>一連のフレームプロセスの流れを表す <see cref="FrameProcess"/> オブジェクト</returns>
        public static FrameProcess Wait(this FrameProcess frameProcess, int time)
        {
            if(time < 0) { throw new ArgumentException($"Time must be bigger than 0."); }
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
            if(condition == null) { throw new ArgumentNullException(nameof(condition)); }
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
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
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            if(!frameProcess.ProcessObj.IsActivated) { frameProcess.ProcessObj.Activate(FrameProcess.TARGET_LAYER); }
            frameProcess.ProcessObj.AddBehavior(FrameProcess.ALWAYS_TRUE, behavior);
            return frameProcess;
        }

        /// <summary>このフレームプロセスの以降の動作をすべてキャンセルし停止させます</summary>
        public static void Cancel(this FrameProcess frameProcess) => frameProcess.ProcessObj.Cancel();
    }
    #endregion
}
