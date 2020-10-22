#nullable enable
using System;

namespace Elffy.InputSystem
{
    /// <summary>key modifiers of keyborad</summary>
    [Flags]
    public enum KeyModifiers
    {
        // values are same as OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers

        None = 0,
        Shift = 1,
        Control = 2,
        Alt = 4,
        Super = 8,
        CapsLock = 16,
        NumLock = 32
    }
}
