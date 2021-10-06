#nullable enable
using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>Life states of <see cref="FrameObject"/></summary>
    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue("New", 0, "Initial state of <see cref=\"" + nameof(FrameObject) + "\"/>. Not managed by the engine.")]
    [EnumLikeValue("Activating", 1)]
    [EnumLikeValue("Activated", 2, "State that <see cref=\"" + nameof(FrameObject) + "\"/> is in the activation queue. (It is not running yet, it gets alive in the next frame.)")]
    [EnumLikeValue("Alive", 3, "State that <see cref=\"" + nameof(FrameObject) + "\"/> is running.")]
    [EnumLikeValue("Terminated", 4, "State that <see cref=\"" + nameof(FrameObject) + "\"/> is in the termination queue. (It is still running, it gets dead in the next frame.)")]
    [EnumLikeValue("Dead", 5, "State that <see cref=\"" + nameof(FrameObject) + "\"/> is dead. Not managed by the engine.")]
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
        public bool IsRunning() => this == Alive || this == Terminated;
    }
}
