#nullable enable
using Elffy.Effective.Unsafes;
using OpenToolkit.Windowing.Common.Input;
using OpenToolkit.Windowing.GraphicsLibraryFramework;
using GlfwKeyModifiers = OpenToolkit.Windowing.GraphicsLibraryFramework.KeyModifiers;
using KeyModifiers = OpenToolkit.Windowing.Common.Input.KeyModifiers;

namespace Elffy.OpenGL.Windowing
{
    internal static class GLFWKeyMapper
    {
        private static readonly Key[] _map = GenerateGlfwKeyMapping();

        private static Key[] GenerateGlfwKeyMapping()
        {
            var map = new Key[(int)Keys.LastKey + 1];
            map[(int)Keys.Space] = Key.Space;
            map[(int)Keys.Apostrophe] = Key.Quote;
            map[(int)Keys.Comma] = Key.Comma;
            map[(int)Keys.Minus] = Key.Minus;
            map[(int)Keys.Period] = Key.Period;
            map[(int)Keys.Slash] = Key.Slash;
            map[(int)Keys.D0] = Key.Number0;
            map[(int)Keys.D1] = Key.Number1;
            map[(int)Keys.D2] = Key.Number2;
            map[(int)Keys.D3] = Key.Number3;
            map[(int)Keys.D4] = Key.Number4;
            map[(int)Keys.D5] = Key.Number5;
            map[(int)Keys.D6] = Key.Number6;
            map[(int)Keys.D7] = Key.Number7;
            map[(int)Keys.D8] = Key.Number8;
            map[(int)Keys.D9] = Key.Number9;
            map[(int)Keys.Semicolon] = Key.Semicolon;
            map[(int)Keys.Equal] = Key.Plus;
            map[(int)Keys.A] = Key.A;
            map[(int)Keys.B] = Key.B;
            map[(int)Keys.C] = Key.C;
            map[(int)Keys.D] = Key.D;
            map[(int)Keys.E] = Key.E;
            map[(int)Keys.F] = Key.F;
            map[(int)Keys.G] = Key.G;
            map[(int)Keys.H] = Key.H;
            map[(int)Keys.I] = Key.I;
            map[(int)Keys.J] = Key.J;
            map[(int)Keys.K] = Key.K;
            map[(int)Keys.L] = Key.L;
            map[(int)Keys.M] = Key.M;
            map[(int)Keys.N] = Key.N;
            map[(int)Keys.O] = Key.O;
            map[(int)Keys.P] = Key.P;
            map[(int)Keys.Q] = Key.Q;
            map[(int)Keys.R] = Key.R;
            map[(int)Keys.S] = Key.S;
            map[(int)Keys.T] = Key.T;
            map[(int)Keys.U] = Key.U;
            map[(int)Keys.V] = Key.V;
            map[(int)Keys.W] = Key.W;
            map[(int)Keys.X] = Key.X;
            map[(int)Keys.Y] = Key.Y;
            map[(int)Keys.Z] = Key.Z;
            map[(int)Keys.LeftBracket] = Key.BracketLeft;
            map[(int)Keys.Backslash] = Key.BackSlash;
            map[(int)Keys.RightBracket] = Key.BracketRight;
            map[(int)Keys.GraveAccent] = Key.Grave;

            // TODO: What are these world keys and how do I handle them.
            // map[(int)Keys.World1] = Key.Z;
            // map[(int)Keys.World2] = Key.Z;
            map[(int)Keys.Escape] = Key.Escape;
            map[(int)Keys.Enter] = Key.Enter;
            map[(int)Keys.Tab] = Key.Tab;
            map[(int)Keys.Backspace] = Key.BackSpace;
            map[(int)Keys.Insert] = Key.Insert;
            map[(int)Keys.Delete] = Key.Delete;
            map[(int)Keys.Right] = Key.Right;
            map[(int)Keys.Left] = Key.Left;
            map[(int)Keys.Down] = Key.Down;
            map[(int)Keys.Up] = Key.Up;
            map[(int)Keys.PageUp] = Key.PageUp;
            map[(int)Keys.PageDown] = Key.PageDown;
            map[(int)Keys.Home] = Key.Home;
            map[(int)Keys.End] = Key.End;
            map[(int)Keys.CapsLock] = Key.CapsLock;
            map[(int)Keys.ScrollLock] = Key.ScrollLock;
            map[(int)Keys.NumLock] = Key.NumLock;
            map[(int)Keys.PrintScreen] = Key.PrintScreen;
            map[(int)Keys.Pause] = Key.Pause;
            map[(int)Keys.F1] = Key.F1;
            map[(int)Keys.F2] = Key.F2;
            map[(int)Keys.F3] = Key.F3;
            map[(int)Keys.F4] = Key.F4;
            map[(int)Keys.F5] = Key.F5;
            map[(int)Keys.F6] = Key.F6;
            map[(int)Keys.F7] = Key.F7;
            map[(int)Keys.F8] = Key.F8;
            map[(int)Keys.F9] = Key.F9;
            map[(int)Keys.F10] = Key.F10;
            map[(int)Keys.F11] = Key.F11;
            map[(int)Keys.F12] = Key.F12;
            map[(int)Keys.F13] = Key.F13;
            map[(int)Keys.F14] = Key.F14;
            map[(int)Keys.F15] = Key.F15;
            map[(int)Keys.F16] = Key.F16;
            map[(int)Keys.F17] = Key.F17;
            map[(int)Keys.F18] = Key.F18;
            map[(int)Keys.F19] = Key.F19;
            map[(int)Keys.F20] = Key.F20;
            map[(int)Keys.F21] = Key.F21;
            map[(int)Keys.F22] = Key.F22;
            map[(int)Keys.F23] = Key.F23;
            map[(int)Keys.F24] = Key.F24;
            map[(int)Keys.F25] = Key.F25;
            map[(int)Keys.KeyPad0] = Key.Keypad0;
            map[(int)Keys.KeyPad1] = Key.Keypad1;
            map[(int)Keys.KeyPad2] = Key.Keypad2;
            map[(int)Keys.KeyPad3] = Key.Keypad3;
            map[(int)Keys.KeyPad4] = Key.Keypad4;
            map[(int)Keys.KeyPad5] = Key.Keypad5;
            map[(int)Keys.KeyPad6] = Key.Keypad6;
            map[(int)Keys.KeyPad7] = Key.Keypad7;
            map[(int)Keys.KeyPad8] = Key.Keypad8;
            map[(int)Keys.KeyPad9] = Key.Keypad9;
            map[(int)Keys.KeyPadDecimal] = Key.KeypadDecimal;
            map[(int)Keys.KeyPadDivide] = Key.KeypadDivide;
            map[(int)Keys.KeyPadMultiply] = Key.KeypadMultiply;
            map[(int)Keys.KeyPadSubtract] = Key.KeypadSubtract;
            map[(int)Keys.KeyPadAdd] = Key.KeypadAdd;
            map[(int)Keys.KeyPadEnter] = Key.KeypadEnter;
            map[(int)Keys.KeyPadEqual] = Key.KeypadEqual;
            map[(int)Keys.LeftShift] = Key.ShiftLeft;
            map[(int)Keys.LeftControl] = Key.ControlLeft;
            map[(int)Keys.LeftAlt] = Key.AltLeft;
            map[(int)Keys.LeftSuper] = Key.WinLeft;
            map[(int)Keys.RightShift] = Key.ShiftRight;
            map[(int)Keys.RightControl] = Key.ControlRight;
            map[(int)Keys.RightAlt] = Key.AltRight;
            map[(int)Keys.RightSuper] = Key.WinRight;
            map[(int)Keys.Menu] = Key.Menu;
            return map;
        }


        public static Key Map(Keys glfwKey)
        {
            var index = (int)glfwKey;
            var map = _map;
            if((uint)index < (uint)map.Length) {
                return map.At(index);
            }
            else {
                return Key.Unknown;
            }
        }

        public static KeyModifiers Map(GlfwKeyModifiers mod)
        {
            KeyModifiers value = default;

            // I avoid Enum.Hasflag (though it has no problem in .net core 3.1)

            if((mod & GlfwKeyModifiers.Alt) == GlfwKeyModifiers.Alt) {
                value |= KeyModifiers.Alt;
            }

            if((mod & GlfwKeyModifiers.Shift) == GlfwKeyModifiers.Shift) {
                value |= KeyModifiers.Shift;
            }

            if((mod & GlfwKeyModifiers.Control) == GlfwKeyModifiers.Control) {
                value |= KeyModifiers.Control;
            }

            if((mod & GlfwKeyModifiers.Super) == GlfwKeyModifiers.Super) {
                value |= KeyModifiers.Command;
            }

            return value;
        }
    }
}
