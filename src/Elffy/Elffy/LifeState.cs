#nullable enable

namespace Elffy
{
    /// <summary>Life states of <see cref="FrameObject"/></summary>
    public enum LifeState : byte
    {
        /// <summary>Initial state of <see cref="FrameObject"/>. Not managed by the engine.</summary>
        New = 0,
        /// <summary>State that <see cref="FrameObject"/> is in the activation queue. (It is not running yet, it gets alive in the next frame.)</summary>
        Activated,
        /// <summary>State that <see cref="FrameObject"/> is running.</summary>
        Alive,
        /// <summary>State that <see cref="FrameObject"/> is in the termination queue. (It is still running, it gets dead in the next frame.)</summary>
        Terminated,
        /// <summary>State that <see cref="FrameObject"/> is dead. Not managed by the engine.</summary>
        Dead,
    }
}
