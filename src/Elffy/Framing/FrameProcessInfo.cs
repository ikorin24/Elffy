#nullable enable
using Elffy.Effective.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elffy.Framing
{
    /// <summary><see cref="FrameProcess"/> の現在実行中の処理に渡される情報</summary>
    public readonly struct FrameProcessInfo : IEquatable<FrameProcessInfo>
    {
        #region member

        /// <summary>現在の <see cref="FrameProcessBehavior"/> の寿命</summary>
        public readonly TimeSpan LifeSpan;

        /// <summary>現在の <see cref="FrameProcessBehavior"/> の寿命フレーム</summary>
        public readonly int FrameSpan;

        /// <summary>現在の <see cref="FrameProcessBehavior"/> が始まってからのフレーム数</summary>
        public readonly int FrameNum;

        /// <summary>現在の <see cref="FrameProcessBehavior"/> が始まってからの時間</summary>
        public readonly TimeSpan Time;

        /// <summary><see cref="Mode"/> が <see cref="FrameProcessEndMode.Condition"/> 現在の場合、この <see cref="FrameProcessBehavior"/> の終了条件を返します</summary>
        public readonly Func<bool>? Condition;

        /// <summary>この <see cref="FrameProcessBehavior"/> の終了モード</summary>
        public readonly FrameProcessEndMode Mode;

        #endregion

        #region constructor

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FrameProcessInfo(TimeSpan lifeSpan)
        {
            LifeSpan = lifeSpan;
            FrameNum = 0;
            FrameSpan = 0;
            Mode = FrameProcessEndMode.LifeSpen;
            Condition = null;
            Time = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FrameProcessInfo(int frameSpan)
        {
            LifeSpan = default;
            FrameNum = 0;
            FrameSpan = frameSpan;
            Mode = FrameProcessEndMode.FrameSpan;
            Condition = null;
            Time = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FrameProcessInfo(Func<bool> condition)
        {
            LifeSpan = default;
            FrameNum = 0;
            FrameSpan = 0;
            Mode = FrameProcessEndMode.Condition;
            Condition = condition;
            Time = default;
        }

        #endregion

        #region method and property

        internal bool IsEnd
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Mode switch
            {
                FrameProcessEndMode.LifeSpen => Time >= LifeSpan,
                FrameProcessEndMode.Condition => !Condition!(),
                FrameProcessEndMode.FrameSpan => FrameNum >= FrameSpan,
                _ => true,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetTime(TimeSpan value) => UnsafeTool.SetValue(Time, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetFrameNum(int value) => UnsafeTool.SetValue(FrameNum, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static FrameProcessInfo LifeSpanMode(TimeSpan lifeSpan) => new FrameProcessInfo(lifeSpan);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static FrameProcessInfo FrameSpanMode(int frameSpan) => new FrameProcessInfo(frameSpan);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static FrameProcessInfo ConditionalMode(Func<bool> condition) => new FrameProcessInfo(condition);



        public override bool Equals(object? obj) => obj is FrameProcessInfo info && Equals(info);

        public bool Equals(FrameProcessInfo other)
        {
            return LifeSpan.Equals(other.LifeSpan) &&
                   FrameSpan == other.FrameSpan &&
                   FrameNum == other.FrameNum &&
                   Time.Equals(other.Time) &&
                   EqualityComparer<Func<bool>?>.Default.Equals(Condition, other.Condition) &&
                   Mode == other.Mode;
        }

        public override int GetHashCode() => HashCode.Combine(LifeSpan, FrameSpan, FrameNum, Time, Condition, Mode);

        #endregion
    }
}
