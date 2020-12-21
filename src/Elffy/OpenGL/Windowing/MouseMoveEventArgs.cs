#nullable enable

namespace Elffy.OpenGL.Windowing
{
    internal readonly struct MouseMoveEventArgs
    {
        public readonly Vector2 Position;

        internal MouseMoveEventArgs(in Vector2 mousePos)
        {
            Position = mousePos;
        }
    }
}
