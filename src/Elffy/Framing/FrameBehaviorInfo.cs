#nullable enable
using Elffy.Effective.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elffy.Framing
{
    /// <summary><see cref="FrameStream"/> の現在実行中の処理に渡される情報</summary>
    public readonly struct FrameBehaviorInfo : IEquatable<FrameBehaviorInfo>
    {
        #region member

        /// <summary>現在の <see cref="FrameBehaviorDelegate"/> の寿命</summary>
        public readonly TimeSpan LifeSpan;

        /// <summary>現在の <see cref="FrameBehaviorDelegate"/> の寿命フレーム</summary>
        public readonly int FrameSpan;

        /// <summary>現在の <see cref="FrameBehaviorDelegate"/> が始まってからのフレーム数</summary>
        public readonly int FrameNum;

        /// <summary>現在の <see cref="FrameBehaviorDelegate"/> が始まってからの時間</summary>
        public readonly TimeSpan Time;

        /// <summary><see cref="Mode"/> が <see cref="FrameBehaviorEndMode.Conditional"/> 現在の場合、この <see cref="FrameBehaviorDelegate"/> の終了条件を返します</summary>
        public readonly Func<bool>? Condition;

        /// <summary>この <see cref="FrameBehaviorDelegate"/> の終了モード</summary>
        public readonly FrameBehaviorEndMode Mode;

        #endregion

        #region constructor

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FrameBehaviorInfo(TimeSpan lifeSpan)
        {
            LifeSpan = lifeSpan;
            FrameNum = 0;
            FrameSpan = 0;
            Mode = FrameBehaviorEndMode.LifeSpen;
            Condition = null;
            Time = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FrameBehaviorInfo(int frameSpan)
        {
            LifeSpan = default;
            FrameNum = 0;
            FrameSpan = frameSpan;
            Mode = FrameBehaviorEndMode.FrameSpan;
            Condition = null;
            Time = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FrameBehaviorInfo(Func<bool> condition)
        {
            LifeSpan = default;
            FrameNum = 0;
            FrameSpan = 0;
            Mode = FrameBehaviorEndMode.Conditional;
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
                FrameBehaviorEndMode.LifeSpen => Time >= LifeSpan,
                FrameBehaviorEndMode.Conditional => !Condition!(),
                FrameBehaviorEndMode.FrameSpan => FrameNum >= FrameSpan,
                _ => true,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetTime(TimeSpan value) => UnsafeTool.SetValue(Time, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetFrameNum(int value) => UnsafeTool.SetValue(FrameNum, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static FrameBehaviorInfo LifeSpanMode(TimeSpan lifeSpan) => new FrameBehaviorInfo(lifeSpan);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static FrameBehaviorInfo FrameSpanMode(int frameSpan) => new FrameBehaviorInfo(frameSpan);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static FrameBehaviorInfo ConditionalMode(Func<bool> condition) => new FrameBehaviorInfo(condition);



        public override bool Equals(object? obj) => obj is FrameBehaviorInfo info && Equals(info);

        public bool Equals(FrameBehaviorInfo other)
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
