#nullable enable
using System.Runtime.CompilerServices;
using LSV = Elffy.LifeStateValue;

namespace Elffy
{
    /// <summary>Life states of <see cref="FrameObject"/></summary>
    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue(nameof(LSV.New), LSV.New, "public", "Initial state of the object, that is not managed by the engine.")]
    [EnumLikeValue(nameof(LSV.Activating), LSV.Activating, "public", "State that the object is in the activation queue. (It is not running yet, it gets alive in the next frame.)")]
    [EnumLikeValue(nameof(LSV.Alive), LSV.Alive, "public", "State that the object is running.")]
    [EnumLikeValue(nameof(LSV.Terminating), LSV.Terminating, "public", "State that the object is in the termination queue. (It is still running, it gets dead in the next frame.)")]
    [EnumLikeValue(nameof(LSV.Dead), LSV.Dead, "public", "State that the object is dead, that is not managed by the engine.")]
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

    internal static class LifeStateValue
    {
        public const byte New = 0;
        public const byte Activating = 1;
        public const byte Alive = 2;
        public const byte Terminating = 3;
        public const byte Dead = 4;
    }
}
