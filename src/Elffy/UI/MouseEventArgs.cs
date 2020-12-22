#nullable enable

namespace Elffy.UI
{
    /// <summary>Mouse event argument class</summary>
    public readonly struct MouseEventArgs
    {
        /// <summary>mouse position</summary>
        public Vector2 MousePosition { get; }

        /// <summary>constructor</summary>
        /// <param name="mousePosition">mouse position</param>
        public MouseEventArgs(in Vector2 mousePosition)
        {
            MousePosition = mousePosition;
        }
    }
}
