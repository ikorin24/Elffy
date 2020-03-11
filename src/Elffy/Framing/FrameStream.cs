#nullable enable
using Elffy.Core;
using Elffy.Effective.Internal;
using Elffy.Exceptions;
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Framing
{
    /// <summary>一連のフレームストリームの流れを表すオブジェクト</summary>
    public readonly struct FrameStream
    {
        /// <summary><see cref="StreamObj"/> を有効にするレイヤー</summary>
        internal static SystemLayer TARGET_LAYER => Engine.CurrentScreen.Layers.SystemLayer;

        /// <summary>何もしない動作を表す <see cref="FrameBehaviorDelegate"/> オブジェクト</summary>
        internal static readonly FrameBehaviorDelegate WAIT_BEHAVIOR = info => { };
        /// <summary>常にtrueを返す条件</summary>
        internal static readonly Func<bool> ALWAYS_TRUE = () => true;

        #pragma warning disable CS0649
        private readonly FrameStreamObject? _streamObj;
        #pragma warning restore CS0649

        /// <summary>この <see cref="FrameStream"/> の実体</summary>
        internal readonly FrameStreamObject StreamObj => _streamObj ?? UnsafeTool.SetValue(_streamObj!, new FrameStreamObject());

        /// <summary>指定した時間だけ、指定した処理を毎フレーム実行します</summary>
        /// <param name="time">実行時間</param>
        /// <param name="action">実行する処理</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        public static FrameStream Begin(TimeSpan time, FrameBehaviorDelegate action) => new FrameStream().Begin(time, action);

        /// <summary>指定した処理を次のフレームに実行します</summary>
        /// <param name="action">実行する処理</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        public static FrameStream Do(FrameBehaviorDelegate action) => new FrameStream().Do(action);

        /// <summary>指定した時間、フレームストリームを停止します</summary>
        /// <param name="time">停止時間</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        public static FrameStream Wait(TimeSpan time) => new FrameStream().Wait(time);

        /// <summary>指定した条件を満たす間、指定した処理を毎フレーム実行します</summary>
        /// <param name="condition">処理の継続条件</param>
        /// <param name="action">実行する処理</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        public static FrameStream While(Func<bool> condition, FrameBehaviorDelegate action) => new FrameStream().While(condition, action);

        /// <summary>指定した処理を毎フレーム実行し続けます</summary>
        /// <param name="action">実行する処理</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        public static FrameStream WhileTrue(FrameBehaviorDelegate action) => new FrameStream().WhileTrue(action);
    }

    public static class FrameStreamExtension
    {
        /// <summary>指定した時間だけ、指定した処理を毎フレーム実行します</summary>
        /// <param name="frameStream">動作を追加するフレームストリーム</param>
        /// <param name="time">実行時間</param>
        /// <param name="action">実行する処理</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameStream Begin(this FrameStream frameStream, TimeSpan time, FrameBehaviorDelegate action)
        {
            ArgumentChecker.ThrowArgumentIf(time < TimeSpan.Zero, $"{nameof(time)} is negative.");
            ArgumentChecker.ThrowIfNullArg(action, nameof(action));

            var obj = frameStream.StreamObj;
            obj.Activate(FrameStream.TARGET_LAYER);
            obj.AddLifeSpanBehavior(time, action);
            return frameStream;
        }

        /// <summary>指定した処理を次のフレームに実行します</summary>
        /// <param name="frameStream">動作を追加するフレームストリーム</param>
        /// <param name="action">実行する処理</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameStream Do(this FrameStream frameStream, FrameBehaviorDelegate action)
        {
            ArgumentChecker.ThrowIfNullArg(action, nameof(action));

            var obj = frameStream.StreamObj;
            obj.Activate(FrameStream.TARGET_LAYER);
            obj.AddFrameSpanBehavior(1, action);
            return frameStream;
        }

        /// <summary>指定した時間、フレームストリームを停止します</summary>
        /// <param name="frameStream">動作を追加するフレームストリーム</param>
        /// <param name="time">停止時間</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameStream Wait(this FrameStream frameStream, TimeSpan time)
        {
            ArgumentChecker.ThrowArgumentIf(time < TimeSpan.Zero, $"{nameof(time)} is negative.");

            var obj = frameStream.StreamObj;
            obj.Activate(FrameStream.TARGET_LAYER);
            obj.AddLifeSpanBehavior(time, FrameStream.WAIT_BEHAVIOR);
            return frameStream;
        }

        /// <summary>指定した条件を満たす間、指定した処理を毎フレーム実行します</summary>
        /// <param name="frameStream">動作を追加するフレームストリーム</param>
        /// <param name="condition">処理の継続条件</param>
        /// <param name="action">実行する処理</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameStream While(this FrameStream frameStream, Func<bool> condition, FrameBehaviorDelegate action)
        {
            ArgumentChecker.ThrowIfNullArg(condition, nameof(condition));
            ArgumentChecker.ThrowIfNullArg(action, nameof(action));

            var obj = frameStream.StreamObj;
            obj.Activate(FrameStream.TARGET_LAYER);
            obj.AddConditionalBehavior(condition, action);
            return frameStream;
        }

        /// <summary>指定した処理を毎フレーム実行し続けます</summary>
        /// <param name="frameStream">動作を追加するフレームストリーム</param>
        /// <param name="action">実行する処理</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameStream WhileTrue(this FrameStream frameStream, FrameBehaviorDelegate action)
        {
            ArgumentChecker.ThrowIfNullArg(action, nameof(action));

            var obj = frameStream.StreamObj;
            obj.Activate(FrameStream.TARGET_LAYER);
            obj.AddConditionalBehavior(FrameStream.ALWAYS_TRUE, action);
            return frameStream;
        }

        /// <summary>このフレームストリームの以降の動作をすべてキャンセルし停止させます</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Cancel(this FrameStream frameStream) => frameStream.StreamObj.Cancel();
    }
}
