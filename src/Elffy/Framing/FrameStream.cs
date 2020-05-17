#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Framing
{
    /// <summary>一連のフレームストリームの流れを表すオブジェクト</summary>
    public readonly struct FrameStream : IDisposable
    {
        private readonly FrameStreamObject _streamObj;

        private FrameStreamObject StreamObj
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _streamObj ?? throw new ObjectDisposedException(nameof(FrameStream));
        }

        public readonly bool IsFrosen => (_streamObj == null);

        private FrameStream(IHostScreen screen)
        {
            _streamObj = new FrameStreamObject(screen);   // ほんとはプールから取ってきたい
        }

        public static FrameStream GetStream(IHostScreen screen) => new FrameStream(screen);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrameStream Do(TimeSpan time, FrameBehaviorDelegate action)
        {
            if(time < TimeSpan.Zero) { throw new ArgumentException($"{nameof(time)} is negative."); }
            if(action is null) { throw new ArgumentNullException(nameof(action)); }
            var obj = StreamObj;
            obj.Activate();
            obj.AddLifeSpanBehavior(time, action);
            return this;
        }

        /// <summary>指定した処理を次のフレームに実行します</summary>
        /// <param name="action">実行する処理</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrameStream Do(FrameBehaviorDelegate action)
        {
            if(action is null) { throw new ArgumentNullException(nameof(action)); }
            var obj = StreamObj;
            obj.Activate();
            obj.AddFrameSpanBehavior(1, action);
            return this;
        }

        /// <summary>指定した時間、処理をを停止します</summary>
        /// <param name="time">停止時間</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrameStream Wait(TimeSpan time)
        {
            if(time < TimeSpan.Zero) { throw new ArgumentOutOfRangeException($"{nameof(time)} is negative."); }
            var obj = StreamObj;
            obj.Activate();
            obj.AddLifeSpanBehavior(time, _ => { });
            return this;
        }

        /// <summary>指定した条件を満たす間、指定した処理を毎フレーム実行します</summary>
        /// <param name="condition">処理の継続条件</param>
        /// <param name="action">実行する処理</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrameStream While(Func<bool> condition, FrameBehaviorDelegate action)
        {
            if(condition is null) { throw new ArgumentNullException(nameof(condition)); }
            if(action is null) { throw new ArgumentNullException(nameof(action)); }
            var obj = StreamObj;
            obj.Activate();
            obj.AddConditionalBehavior(condition, action);
            return this;
        }

        /// <summary>指定した処理を毎フレーム実行し続けます</summary>
        /// <param name="action">実行する処理</param>
        /// <returns>一連のフレームストリームの流れを表す <see cref="FrameStream"/> オブジェクト</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrameStream Endless(FrameBehaviorDelegate action)
        {
            if(action is null) { throw new ArgumentNullException(nameof(action)); }
            var obj = StreamObj;
            obj.Activate();
            obj.AddConditionalBehavior(() => true, action);
            return this;
        }

        /// <summary>処理を全てキャンセルし停止させます。このメソッドが呼ばれた時点で残りの処理に関わらず即終了します</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cancel()
        {
            var obj = StreamObj;
            obj.Cancel();
            Unsafe.AsRef(_streamObj) = null!;
        }

        /// <summary>終了処理を追加します。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if(IsFrosen) { return; }
            var obj = StreamObj;
            obj.AddEndBehavior();
            Unsafe.AsRef(_streamObj) = null!;
        }
    }
}
