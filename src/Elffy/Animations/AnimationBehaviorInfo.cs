#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elffy.Animations
{
    /// <summary><see cref="Animation"/> の現在実行中の処理に渡される情報</summary>
    public readonly struct AnimationBehaviorInfo : IEquatable<AnimationBehaviorInfo>
    {
        /// <summary>現在の <see cref="AnimationBehaviorDelegate"/> の寿命</summary>
        public readonly TimeSpan LifeSpan;

        /// <summary>現在の <see cref="AnimationBehaviorDelegate"/> の寿命フレーム</summary>
        public readonly int FrameSpan;

        /// <summary>現在の <see cref="AnimationBehaviorDelegate"/> が始まってからのフレーム数</summary>
        public readonly int FrameNum;

        /// <summary>現在の <see cref="AnimationBehaviorDelegate"/> が始まってからの時間</summary>
        public readonly TimeSpan Time;

        /// <summary><see cref="Mode"/> が <see cref="AnimationBehaviorEndMode.Conditional"/> 現在の場合、この <see cref="AnimationBehaviorDelegate"/> の終了条件を返します</summary>
        public readonly Func<bool>? Condition;

        /// <summary>この <see cref="AnimationBehaviorDelegate"/> の終了モード</summary>
        public readonly AnimationBehaviorEndMode Mode;



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AnimationBehaviorInfo(TimeSpan lifeSpan)
        {
            LifeSpan = lifeSpan;
            FrameNum = 0;
            FrameSpan = 0;
            Mode = AnimationBehaviorEndMode.LifeSpen;
            Condition = null;
            Time = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AnimationBehaviorInfo(int frameSpan)
        {
            LifeSpan = default;
            FrameNum = 0;
            FrameSpan = frameSpan;
            Mode = AnimationBehaviorEndMode.FrameSpan;
            Condition = null;
            Time = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AnimationBehaviorInfo(Func<bool> condition)
        {
            LifeSpan = default;
            FrameNum = 0;
            FrameSpan = 0;
            Mode = AnimationBehaviorEndMode.Conditional;
            Condition = condition;
            Time = default;
        }


        internal bool IsEnd
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Mode switch
            {
                AnimationBehaviorEndMode.LifeSpen => Time >= LifeSpan,
                AnimationBehaviorEndMode.Conditional => !Condition!(),
                AnimationBehaviorEndMode.FrameSpan => FrameNum >= FrameSpan,
                _ => true,
            };
        }

        internal void SetTime(TimeSpan value) => Unsafe.AsRef(Time) = value;

        internal void SetFrameNum(int value) => Unsafe.AsRef(FrameNum) = value;

        internal static AnimationBehaviorInfo LifeSpanMode(TimeSpan lifeSpan) => new AnimationBehaviorInfo(lifeSpan);

        internal static AnimationBehaviorInfo FrameSpanMode(int frameSpan) => new AnimationBehaviorInfo(frameSpan);

        internal static AnimationBehaviorInfo ConditionalMode(Func<bool> condition) => new AnimationBehaviorInfo(condition);



        public override bool Equals(object? obj) => obj is AnimationBehaviorInfo info && Equals(info);

        public bool Equals(AnimationBehaviorInfo other)
        {
            return LifeSpan.Equals(other.LifeSpan) &&
                   FrameSpan == other.FrameSpan &&
                   FrameNum == other.FrameNum &&
                   Time.Equals(other.Time) &&
                   EqualityComparer<Func<bool>?>.Default.Equals(Condition, other.Condition) &&
                   Mode == other.Mode;
        }

        public override int GetHashCode() => HashCode.Combine(LifeSpan, FrameSpan, FrameNum, Time, Condition, Mode);

    }
}
