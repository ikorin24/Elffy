#nullable enable
using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using OpenToolkit.Windowing.GraphicsLibraryFramework;
using OpenToolkit.Windowing.Common;

namespace Elffy.OpenGL
{
    internal unsafe partial class WindowGLFW
    {
        private bool _callbackRegistered;

        private GLFWCallbacks.WindowCloseCallback? _closeCallback;
        private GLFWCallbacks.WindowPosCallback? _posCallback;
        private GLFWCallbacks.WindowSizeCallback? _sizeCallback;
        private GLFWCallbacks.WindowIconifyCallback? _iconifyCallback;
        private GLFWCallbacks.WindowFocusCallback? _focusCallback;
        private GLFWCallbacks.CharCallback? _charCallback;
        private GLFWCallbacks.KeyCallback? _keyCallback;
        private GLFWCallbacks.CursorEnterCallback? _cursorEnterCallback;
        private GLFWCallbacks.MouseButtonCallback? _mouseButtonCallback;
        private GLFWCallbacks.CursorPosCallback? _cursorPosCallback;
        private GLFWCallbacks.ScrollCallback? _scrollCallback;
        private GLFWCallbacks.DropCallback? _dropCallback;
        private GLFWCallbacks.JoystickCallback? _joystickCallback;
        private GLFWCallbacks.MonitorCallback? _monitorCallback;
        private GLFWCallbacks.WindowRefreshCallback? _refreshCallback;


        public event Action<WindowGLFW>? Load;

        //public event Action<WindowGLFW>? Unload;

        public event Action<WindowGLFW, FrameEventArgs>? UpdateFrame;

        //public event Action<WindowGLFW, FrameEventArgs>? RenderFrame;

        public event Action<WindowGLFW, WindowPositionEventArgs>? Move;
        public event Action<WindowGLFW, ResizeEventArgs>? Resize;

        public event Action<WindowGLFW>? Refresh;

        public event Action<WindowGLFW, CancelEventArgs>? Closing;
        public event Action<WindowGLFW>? Closed;

        public event Action<WindowGLFW, MinimizedEventArgs>? Minimized;

        public event Action<WindowGLFW, JoystickEventArgs>? JoystickConnected;

        public event Action<WindowGLFW, FocusedChangedEventArgs>? FocusedChanged;

        public event Action<WindowGLFW, TextInputEventArgs>? TextInput;

        public event Action<WindowGLFW, KeyboardKeyEventArgs>? KeyDown;
        public event Action<WindowGLFW, KeyboardKeyEventArgs>? KeyUp;

        public event Action<WindowGLFW, MonitorEventArgs>? MonitorConnected;

        public event Action<WindowGLFW>? MouseLeave;
        public event Action<WindowGLFW>? MouseEnter;
        public event Action<WindowGLFW, MouseButtonEventArgs>? MouseDown;
        public event Action<WindowGLFW, MouseButtonEventArgs>? MouseUp;
        public event Action<WindowGLFW, MouseMoveEventArgs>? MouseMove;
        public event Action<WindowGLFW, MouseWheelEventArgs>? MouseWheel;

        public event FileDropEventHandler? FileDrop;

        private void RegisterWindowCallbacks()
        {
            if(_callbackRegistered) { return; }
            _callbackRegistered = true;

            // [NOTE]
            // Do not register callback to GLFW as lambda or method. (That cannot work)
            // Put delegate on a field and register it to GLFW.

            _posCallback = (_, x, y) =>
            {
                _location = new Vector2i(x, y);
                Move?.Invoke(this, new WindowPositionEventArgs(x, y));
            };
            GLFW.SetWindowPosCallback(_window, _posCallback);

            _sizeCallback = (_, width, height) =>
            {
                GLFW.GetWindowFrameSize(_window, out var left, out var top, out var right, out var bottom);
                _clientSize = new Vector2i(width, height);
                _size = new Vector2i(_clientSize.X + left + right, _clientSize.Y + top + bottom);
                Resize?.Invoke(this, new ResizeEventArgs(width, height));
            };
            GLFW.SetWindowSizeCallback(_window, _sizeCallback);

            _closeCallback = _ =>
            {
                var e = new CancelEventArgs();
                Closing?.Invoke(this, e);
                if(e.Cancel) {
                    GLFW.SetWindowShouldClose(_window, false);
                    return;
                }
                else {
                    _isCloseRequested = true;
                    return;
                }
            };
            GLFW.SetWindowCloseCallback(_window, _closeCallback);


            _iconifyCallback = (_, minimized) =>
            {
                Debug.WriteLine("Iconify Not Impl");    // TODO:
                //Minimized?.Invoke(this, new MinimizedEventArgs(minimized));
            };
            GLFW.SetWindowIconifyCallback(_window, _iconifyCallback);

            _focusCallback = (_, focused) =>
            {
                Debug.WriteLine("Focus Not Impl");    // TODO:
            };
            GLFW.SetWindowFocusCallback(_window, _focusCallback);

            _charCallback = (_, codepoint) =>
            {
                Debug.WriteLine("Set char Not Impl");    // TODO:
            };
            GLFW.SetCharCallback(_window, _charCallback);

            _keyCallback = (_, key, scanCode, action, mods) =>
            {
                Debug.WriteLine("Key call Not Impl");    // TODO:
            };
            GLFW.SetKeyCallback(_window, _keyCallback);

            _cursorEnterCallback = (_, entered) =>
            {
                Debug.WriteLine("Curosr Enter Not Impl");    // TODO:
            };
            GLFW.SetCursorEnterCallback(_window, _cursorEnterCallback);

            _mouseButtonCallback = (_, button, action, mods) =>
            {
                Debug.WriteLine("Mouse Button Not Impl");    // TODO:
            };
            GLFW.SetMouseButtonCallback(_window, _mouseButtonCallback);

            _cursorPosCallback = (_, x, y) =>
            {
                Debug.WriteLine("Cursor Pos Not Impl");    // TODO:
            };
            GLFW.SetCursorPosCallback(_window, _cursorPosCallback);

            _scrollCallback = (_, x, y) =>
            {
                Debug.WriteLine("Scroll Not Impl");    // TODO:
            };
            GLFW.SetScrollCallback(_window, _scrollCallback);

            _dropCallback = (_, count, paths) =>
            {
                Utf8StringRef* files;
                var isHeap = count > 16;
                if(isHeap) {
                    files = (Utf8StringRef*)Marshal.AllocHGlobal(count * sizeof(Utf8StringRef));
                }
                else {
                    var p = stackalloc Utf8StringRef[count];
                    files = p;
                }
                try {
                    for(int i = 0; i < count; i++) {
                        files[i] = new Utf8StringRef(paths[i]);
                    }
                    FileDrop?.Invoke(this, new Utf8StringRefArray(files, count));
                }
                finally {
                    if(isHeap) {
                        Marshal.FreeHGlobal((IntPtr)files);
                    }
                }
            };
            GLFW.SetDropCallback(_window, _dropCallback);

            _joystickCallback = (joystick, eventCode) =>
            {
                Debug.WriteLine("Joystick Not Impl");    // TODO:
            };
            GLFW.SetJoystickCallback(_joystickCallback);

            _monitorCallback = (monitor, state) =>
            {
                Debug.WriteLine("Monitor Not Impl");    // TODO:
            };
            GLFW.SetMonitorCallback(_monitorCallback);

            _refreshCallback = _ =>
            {
                Debug.WriteLine("Refresh Not Impl");    // TODO:
            };
            GLFW.SetWindowRefreshCallback(_window, _refreshCallback);
        }
    }
}
