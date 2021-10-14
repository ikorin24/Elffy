#nullable enable
using System.Runtime.CompilerServices;
using LSV = Elffy.LifeStateValue;

namespace Elffy
{
    /// <summary>Life states of <see cref="FrameObject"/></summary>
    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue(nameof(LSV.New), LSV.New, "Initial state of <see cref=\"" + nameof(FrameObject) + "\"/>. Not managed by the engine.")]
    [EnumLikeValue(nameof(LSV.Activating), LSV.Activating)]
    [EnumLikeValue(nameof(LSV.Alive), LSV.Alive, "State that <see cref=\"" + nameof(FrameObject) + "\"/> is running.")]
    [EnumLikeValue(nameof(LSV.Terminating), LSV.Terminating, "State that <see cref=\"" + nameof(FrameObject) + "\"/> is in the termination queue. (It is still running, it gets dead in the next frame.)")]
    [EnumLikeValue(nameof(LSV.Dead), LSV.Dead, "State that <see cref=\"" + nameof(FrameObject) + "\"/> is dead. Not managed by the engine.")]
    public partial struct LifeState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Is(LifeState state) => _value == state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsBefore(LifeState state) => _value < state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSameOrBefore(LifeState state) => _value <= state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAfter(LifeState state) => _value > state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSameOrAfter(LifeState state) => _value >= state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRunning() => _value == Alive._value || _value == Terminating._value;
    }

    internal static class LifeStateValue
    {
        public const byte New = 0;
        public const byte Activating = 1;
        public const byte Alive = 2;
        public const byte Terminating = 3;
        public const byte Dead = 4;
    }
}
