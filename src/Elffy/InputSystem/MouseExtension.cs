#nullable enable
using System.Runtime.CompilerServices;

namespace Elffy.InputSystem
{
    public static class MouseExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeftDown(this Mouse mouse) => mouse.IsDown(MouseButton.Left);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRightDown(this Mouse mouse) => mouse.IsDown(MouseButton.Right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMiddleDown(this Mouse mouse) => mouse.IsDown(MouseButton.Middle);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeftPressed(this Mouse mouse) => mouse.IsPressed(MouseButton.Left);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRightPressed(this Mouse mouse) => mouse.IsPressed(MouseButton.Right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMiddlePressed(this Mouse mouse) => mouse.IsPressed(MouseButton.Middle);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeftUp(this Mouse mouse) => mouse.IsUp(MouseButton.Left);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRightUp(this Mouse mouse) => mouse.IsUp(MouseButton.Right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMiddleUp(this Mouse mouse) => mouse.IsUp(MouseButton.Middle);
    }
}
