#nullable enable

using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>Life states of <see cref="FrameObject"/></summary>
    public enum LifeState : byte
    {
        /// <summary>Initial state of <see cref="FrameObject"/>. Not managed by the engine.</summary>
        New = 0,

        Activating = 1,
        /// <summary>State that <see cref="FrameObject"/> is in the activation queue. (It is not running yet, it gets alive in the next frame.)</summary>
        Activated = 2,
        /// <summary>State that <see cref="FrameObject"/> is running.</summary>
        Alive = 3,
        /// <summary>State that <see cref="FrameObject"/> is in the termination queue. (It is still running, it gets dead in the next frame.)</summary>
        Terminated = 4,
        /// <summary>State that <see cref="FrameObject"/> is dead. Not managed by the engine.</summary>
        Dead = 5,
    }

    public static class LifeStateExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(this LifeState source, LifeState state)
        {
            return source == state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBefore(this LifeState source, LifeState state)
        {
            return (byte)source < (byte)state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSameOrBefore(this LifeState source, LifeState state)
        {
            return (byte)source <= (byte)state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAfter(this LifeState source, LifeState state)
        {
            return (byte)source > (byte)state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSameOrAfter(this LifeState source, LifeState state)
        {
            return (byte)source >= (byte)state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRunning(this LifeState source)
        {
            return source == LifeState.Alive || source == LifeState.Terminated;
        }
    }
}
