#nullable enable
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elffy.Graphics.OpenGL.Windowing
{
    internal readonly struct JoystickConnectionEventArgs
    {
        private readonly int _joystick;
        private readonly bool _connected;

        internal JoystickConnectionEventArgs(int joystick, bool connected)
        {
            _joystick = joystick;
            _connected = connected;
        }

        //public Joystick ToJoystickInstance()
        //{
        //    var hats = GLFW.GetJoystickHatsRaw(_joystick, out var hatLen);
        //    var axes = GLFW.GetJoystickAxesRaw(_joystick, out var axisLen);
        //    var name = GLFW.GetJoystickName(_joystick);
        //    return new Joystick(name, hats, hatLen, axes, axisLen);
        //}
    }

    //public unsafe sealed class Joystick
    //{
    //    private string _name;
    //    private JoystickHats* _hats;
    //    private int _hatsLength;
    //    private float* _axes;
    //    private int _axisLength;

    //    public string Name => _name;

    //    internal Joystick(string name, JoystickHats* hats, int hatLen, float* axes, int axisLen)
    //    {
    //        _name = name;
    //        _hats = hats;
    //        _hatsLength = hatLen;
    //        _axes = axes;
    //        _axisLength = axisLen;
    //    }

    //    //public JoystickHats GetHatValue(int index)
    //    //{
    //    //    if((uint)index >= (uint)_hatsLength) { ThrowOutOfRange(); }
    //    //    throw new NotImplementedException();

    //    //    static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(index));
    //    //}
    //}
}
