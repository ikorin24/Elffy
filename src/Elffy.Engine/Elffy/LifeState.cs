#nullable enable

using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>Life states of <see cref="FrameObject"/></summary>
    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue("New", 0)]
    [EnumLikeValue("Activating", 1)]
    [EnumLikeValue("Activated", 2)]
    [EnumLikeValue("Alive", 3)]
    [EnumLikeValue("Terminated", 4)]
    [EnumLikeValue("Dead", 5)]
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
