#nullable enable

using System;

namespace Elffy.Core
{
    [Flags]
    internal enum FrameObjectLifeState : uint
    {
        // |   7    | 6 |   5    |    4    |   3   |  2   |      1      |     0      |
        // | pooled |   | frozen | started | alive | dead | terminating | activating |

        /// <summary><see cref="FrameObject"/> の初期状態</summary>
        New = 0,
        /// <summary>activating ビット。Activate が呼ばれてから alive になるまでの間だけ立つ</summary>
        Bit_Activating = 1,
        /// <summary>terminating ビット。Terminate が呼ばれてから alive が下りるまでの間だけ立つ</summary>
        Bit_Terminating = 2,
        /// <summary>dead ビット。</summary>
        Bit_Dead = 4,
        /// <summary>alive ビット。Update 等の処理が毎フレーム呼ばれる状態</summary>
        Bit_Alive = 8,
        /// <summary>started ビット。最初の Start が呼ばれると立つ</summary>
        Bit_Started = 16,

        Bit_Frozen = 32,
        /// <summary>pooled ビット。GC軽減のためにインスタンスがプールに保持されている場合に立つ</summary>
        Bit_Pooled = 128,
    }

    internal static class FrameObjectLifeStateExtension
    {
        public static bool IsNew(this FrameObjectLifeState state) => state == FrameObjectLifeState.New;

        public static bool HasDeadBit(this FrameObjectLifeState state) => HasBit(state, FrameObjectLifeState.Bit_Dead);

        public static bool HasActivatingbit(this FrameObjectLifeState state) => HasBit(state, FrameObjectLifeState.Bit_Activating);

        public static bool HasTerminatingBit(this FrameObjectLifeState state) => HasBit(state, FrameObjectLifeState.Bit_Terminating);

        public static bool HasAliveBit(this FrameObjectLifeState state) => HasBit(state, FrameObjectLifeState.Bit_Alive);

        public static bool HasStartedBit(this FrameObjectLifeState state) => HasBit(state, FrameObjectLifeState.Bit_Started);

        public static bool HasFrozenBit(this FrameObjectLifeState state) => HasBit(state, FrameObjectLifeState.Bit_Frozen);

        public static bool HasPooledBit(this FrameObjectLifeState state) => HasBit(state, FrameObjectLifeState.Bit_Pooled);

        private static bool HasBit(FrameObjectLifeState state, FrameObjectLifeState checkBit)
        {
            return (state & checkBit) == checkBit;
        }
    }
}
