#nullable enable
using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>Life state</summary>
    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue("New", 0, "public", "Initial state of the object, that is not managed by the engine.")]
    [EnumLikeValue("Activating", 1, "public", "State that the object is in the activation queue. (It is not running yet, it gets alive in the next frame.)")]
    [EnumLikeValue("Alive", 2, "public", "State that the object is running.")]
    [EnumLikeValue("Terminating", 3, "public", "State that the object is in the termination queue. (It is still running, it gets dead in the next frame.)")]
    [EnumLikeValue("Dead", 4, "public", "State that the object is dead, that is not managed by the engine.")]
    public partial struct LifeState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRunning() => _value == Alive._value || _value == Terminating._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(LifeState left, LifeState right) => left._value < right._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(LifeState left, LifeState right) => left._value > right._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(LifeState left, LifeState right) => left._value <= right._value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(LifeState left, LifeState right) => left._value >= right._value;
    }
}
