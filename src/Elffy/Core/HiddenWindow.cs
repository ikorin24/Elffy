#nullable enable
using System;
using OpenTK.Windowing.GraphicsLibraryFramework;
using GLFWWindow = OpenTK.Windowing.GraphicsLibraryFramework.Window;

namespace Elffy.Core
{
    internal unsafe class HiddenWindow : IDisposable
    {
        private GLFWWindow* _window;

        public HiddenWindow()
        {
            GLFW.WindowHint(WindowHintBool.Visible, false);
            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);
            GLFW.WindowHint(WindowHintContextApi.ContextCreationApi, ContextApi.NativeContextApi);
            _window = GLFW.CreateWindow(1, 1, string.Empty, null, null);
            SwitchContext();
        }

        public void Dispose()
        {
            GLFW.DestroyWindow(_window);
            _window = null;
        }

        public void SwitchContext()
        {
            GLFW.MakeContextCurrent(_window);
        }
    }
}
