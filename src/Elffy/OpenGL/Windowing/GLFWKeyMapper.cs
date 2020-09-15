#nullable enable
using Elffy.Effective.Unsafes;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Common.Input;
using GlfwKeys = OpenToolkit.Windowing.GraphicsLibraryFramework.Keys;
using GlfwKeyModifiers = OpenToolkit.Windowing.GraphicsLibraryFramework.KeyModifiers;
using KeyModifiers = OpenToolkit.Windowing.Common.Input.KeyModifiers;
using GlfwInputAction = OpenToolkit.Windowing.GraphicsLibraryFramework.InputAction;
using System;

namespace Elffy.OpenGL.Windowing
{
    internal static class GLFWKeyMapper
    {
        private static readonly Key[] _map = GenerateGlfwKeyMapping();

        private static Key[] GenerateGlfwKeyMapping()
        {
            var map = new Key[(int)GlfwKeys.LastKey + 1];
            map[(int)GlfwKeys.Space] = Key.Space;
            map[(int)GlfwKeys.Apostrophe] = Key.Quote;
            map[(int)GlfwKeys.Comma] = Key.Comma;
            map[(int)GlfwKeys.Minus] = Key.Minus;
            map[(int)GlfwKeys.Period] = Key.Period;
            map[(int)GlfwKeys.Slash] = Key.Slash;
            map[(int)GlfwKeys.D0] = Key.Number0;
            map[(int)GlfwKeys.D1] = Key.Number1;
            map[(int)GlfwKeys.D2] = Key.Number2;
            map[(int)GlfwKeys.D3] = Key.Number3;
            map[(int)GlfwKeys.D4] = Key.Number4;
            map[(int)GlfwKeys.D5] = Key.Number5;
            map[(int)GlfwKeys.D6] = Key.Number6;
            map[(int)GlfwKeys.D7] = Key.Number7;
            map[(int)GlfwKeys.D8] = Key.Number8;
            map[(int)GlfwKeys.D9] = Key.Number9;
            map[(int)GlfwKeys.Semicolon] = Key.Semicolon;
            map[(int)GlfwKeys.Equal] = Key.Plus;
            map[(int)GlfwKeys.A] = Key.A;
            map[(int)GlfwKeys.B] = Key.B;
            map[(int)GlfwKeys.C] = Key.C;
            map[(int)GlfwKeys.D] = Key.D;
            map[(int)GlfwKeys.E] = Key.E;
            map[(int)GlfwKeys.F] = Key.F;
            map[(int)GlfwKeys.G] = Key.G;
            map[(int)GlfwKeys.H] = Key.H;
            map[(int)GlfwKeys.I] = Key.I;
            map[(int)GlfwKeys.J] = Key.J;
            map[(int)GlfwKeys.K] = Key.K;
            map[(int)GlfwKeys.L] = Key.L;
            map[(int)GlfwKeys.M] = Key.M;
            map[(int)GlfwKeys.N] = Key.N;
            map[(int)GlfwKeys.O] = Key.O;
            map[(int)GlfwKeys.P] = Key.P;
            map[(int)GlfwKeys.Q] = Key.Q;
            map[(int)GlfwKeys.R] = Key.R;
            map[(int)GlfwKeys.S] = Key.S;
            map[(int)GlfwKeys.T] = Key.T;
            map[(int)GlfwKeys.U] = Key.U;
            map[(int)GlfwKeys.V] = Key.V;
            map[(int)GlfwKeys.W] = Key.W;
            map[(int)GlfwKeys.X] = Key.X;
            map[(int)GlfwKeys.Y] = Key.Y;
            map[(int)GlfwKeys.Z] = Key.Z;
            map[(int)GlfwKeys.LeftBracket] = Key.BracketLeft;
            map[(int)GlfwKeys.Backslash] = Key.BackSlash;
            map[(int)GlfwKeys.RightBracket] = Key.BracketRight;
            map[(int)GlfwKeys.GraveAccent] = Key.Grave;

            // TODO: What are these world keys and how do I handle them.
            // map[(int)Keys.World1] = Key.Z;
            // map[(int)Keys.World2] = Key.Z;
            map[(int)GlfwKeys.Escape] = Key.Escape;
            map[(int)GlfwKeys.Enter] = Key.Enter;
            map[(int)GlfwKeys.Tab] = Key.Tab;
            map[(int)GlfwKeys.Backspace] = Key.BackSpace;
            map[(int)GlfwKeys.Insert] = Key.Insert;
            map[(int)GlfwKeys.Delete] = Key.Delete;
            map[(int)GlfwKeys.Right] = Key.Right;
            map[(int)GlfwKeys.Left] = Key.Left;
            map[(int)GlfwKeys.Down] = Key.Down;
            map[(int)GlfwKeys.Up] = Key.Up;
            map[(int)GlfwKeys.PageUp] = Key.PageUp;
            map[(int)GlfwKeys.PageDown] = Key.PageDown;
            map[(int)GlfwKeys.Home] = Key.Home;
            map[(int)GlfwKeys.End] = Key.End;
            map[(int)GlfwKeys.CapsLock] = Key.CapsLock;
            map[(int)GlfwKeys.ScrollLock] = Key.ScrollLock;
            map[(int)GlfwKeys.NumLock] = Key.NumLock;
            map[(int)GlfwKeys.PrintScreen] = Key.PrintScreen;
            map[(int)GlfwKeys.Pause] = Key.Pause;
            map[(int)GlfwKeys.F1] = Key.F1;
            map[(int)GlfwKeys.F2] = Key.F2;
            map[(int)GlfwKeys.F3] = Key.F3;
            map[(int)GlfwKeys.F4] = Key.F4;
            map[(int)GlfwKeys.F5] = Key.F5;
            map[(int)GlfwKeys.F6] = Key.F6;
            map[(int)GlfwKeys.F7] = Key.F7;
            map[(int)GlfwKeys.F8] = Key.F8;
            map[(int)GlfwKeys.F9] = Key.F9;
            map[(int)GlfwKeys.F10] = Key.F10;
            map[(int)GlfwKeys.F11] = Key.F11;
            map[(int)GlfwKeys.F12] = Key.F12;
            map[(int)GlfwKeys.F13] = Key.F13;
            map[(int)GlfwKeys.F14] = Key.F14;
            map[(int)GlfwKeys.F15] = Key.F15;
            map[(int)GlfwKeys.F16] = Key.F16;
            map[(int)GlfwKeys.F17] = Key.F17;
            map[(int)GlfwKeys.F18] = Key.F18;
            map[(int)GlfwKeys.F19] = Key.F19;
            map[(int)GlfwKeys.F20] = Key.F20;
            map[(int)GlfwKeys.F21] = Key.F21;
            map[(int)GlfwKeys.F22] = Key.F22;
            map[(int)GlfwKeys.F23] = Key.F23;
            map[(int)GlfwKeys.F24] = Key.F24;
            map[(int)GlfwKeys.F25] = Key.F25;
            map[(int)GlfwKeys.KeyPad0] = Key.Keypad0;
            map[(int)GlfwKeys.KeyPad1] = Key.Keypad1;
            map[(int)GlfwKeys.KeyPad2] = Key.Keypad2;
            map[(int)GlfwKeys.KeyPad3] = Key.Keypad3;
            map[(int)GlfwKeys.KeyPad4] = Key.Keypad4;
            map[(int)GlfwKeys.KeyPad5] = Key.Keypad5;
            map[(int)GlfwKeys.KeyPad6] = Key.Keypad6;
            map[(int)GlfwKeys.KeyPad7] = Key.Keypad7;
            map[(int)GlfwKeys.KeyPad8] = Key.Keypad8;
            map[(int)GlfwKeys.KeyPad9] = Key.Keypad9;
            map[(int)GlfwKeys.KeyPadDecimal] = Key.KeypadDecimal;
            map[(int)GlfwKeys.KeyPadDivide] = Key.KeypadDivide;
            map[(int)GlfwKeys.KeyPadMultiply] = Key.KeypadMultiply;
            map[(int)GlfwKeys.KeyPadSubtract] = Key.KeypadSubtract;
            map[(int)GlfwKeys.KeyPadAdd] = Key.KeypadAdd;
            map[(int)GlfwKeys.KeyPadEnter] = Key.KeypadEnter;
            map[(int)GlfwKeys.KeyPadEqual] = Key.KeypadEqual;
            map[(int)GlfwKeys.LeftShift] = Key.ShiftLeft;
            map[(int)GlfwKeys.LeftControl] = Key.ControlLeft;
            map[(int)GlfwKeys.LeftAlt] = Key.AltLeft;
            map[(int)GlfwKeys.LeftSuper] = Key.WinLeft;
            map[(int)GlfwKeys.RightShift] = Key.ShiftRight;
            map[(int)GlfwKeys.RightControl] = Key.ControlRight;
            map[(int)GlfwKeys.RightAlt] = Key.AltRight;
            map[(int)GlfwKeys.RightSuper] = Key.WinRight;
            map[(int)GlfwKeys.Menu] = Key.Menu;
            return map;
        }


        public static Key Map(GlfwKeys glfwKey)
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

        public static InputAction Map(GlfwInputAction action)
        {
            return action switch
            {
                GlfwInputAction.Press => InputAction.Press,
                GlfwInputAction.Release => InputAction.Release,
                GlfwInputAction.Repeat => InputAction.Repeat,
                _ => throw new ArgumentException($"Invalid enum value : {action}"),
            };
        }
    }
}
