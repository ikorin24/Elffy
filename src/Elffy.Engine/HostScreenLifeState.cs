#nullable enable
using System.Runtime.CompilerServices;

namespace Elffy
{
    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue(nameof(LifeStateValue.New), LifeStateValue.New)]
    [EnumLikeValue(nameof(LifeStateValue.Activating), LifeStateValue.Activating)]
    [EnumLikeValue(nameof(LifeStateValue.Alive), LifeStateValue.Alive)]
    [EnumLikeValue(nameof(LifeStateValue.Terminating), LifeStateValue.Terminating)]
    [EnumLikeValue(nameof(LifeStateValue.Dead), LifeStateValue.Dead)]
    public partial struct HostScreenLifeState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Is(HostScreenLifeState state) => _value == state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsBefore(HostScreenLifeState state) => _value < state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSameOrBefore(HostScreenLifeState state) => _value <= state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAfter(HostScreenLifeState state) => _value > state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSameOrAfter(HostScreenLifeState state) => _value >= state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRunning() => _value == Alive._value || _value == Terminating._value;
    }
}
