#nullable enable
using System.Runtime.CompilerServices;
using LSV = Elffy.LifeStateValue;

namespace Elffy
{
    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue(nameof(LSV.New), LSV.New)]
    [EnumLikeValue(nameof(LSV.Activating), LSV.Activating)]
    [EnumLikeValue(nameof(LSV.Alive), LSV.Alive)]
    [EnumLikeValue(nameof(LSV.Terminating), LSV.Terminating)]
    [EnumLikeValue(nameof(LSV.Dead), LSV.Dead)]
    public partial struct LayerLifeState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Is(LayerLifeState state) => _value == state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsBefore(LayerLifeState state) => _value < state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSameOrBefore(LayerLifeState state) => _value <= state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAfter(LayerLifeState state) => _value > state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSameOrAfter(LayerLifeState state) => _value >= state._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRunning() => _value == Alive._value || _value == Terminating._value;
    }
}
