#nullable enable
using System;
using Monitor = OpenTK.Windowing.GraphicsLibraryFramework.Monitor;

namespace Elffy.Graphics.OpenGL.Windowing
{
    internal readonly unsafe struct MonitorConnectionEventArgs
    {
        private readonly IntPtr _monitor;
        private readonly bool _connected;

        internal MonitorConnectionEventArgs(Monitor* monitor, bool connected)
        {
            _monitor = (IntPtr)monitor;
            _connected = connected;
        }
    }
}
