#nullable enable
using System;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Common;
using Elffy.OpenGL.Windowing;
using Elffy.Core;
using GlfwInputAction = OpenTK.Windowing.GraphicsLibraryFramework.InputAction;
using GlfwConnectedState = OpenTK.Windowing.GraphicsLibraryFramework.ConnectedState;
using GLFWCallbacks = OpenTK.Windowing.GraphicsLibraryFramework.GLFWCallbacks;
using GLFW = OpenTK.Windowing.GraphicsLibraryFramework.GLFW;

using MouseMoveEventArgs = Elffy.OpenGL.Windowing.MouseMoveEventArgs;

namespace Elffy.OpenGL
{
    internal unsafe partial class WindowGLFW
    {
        private bool _callbackRegistered;

        private GLFWCallbacks.WindowCloseCallback? _closeCallback;
        private GLFWCallbacks.WindowPosCallback? _posCallback;
        private GLFWCallbacks.WindowSizeCallback? _sizeCallback;
        private GLFWCallbacks.FramebufferSizeCallback? _frameBufferSizeCallback;
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

        public event Action<WindowGLFW, Vector2i>? FrameBufferSizeChanged;

        public event Action<WindowGLFW>? Refresh;

        public event ClosingEventHandler<WindowGLFW>? Closing;

        public event Action<WindowGLFW, MinimizedEventArgs>? Minimized;

        public event Action<WindowGLFW, JoystickConnectionEventArgs>? JoystickConnectionChanged;

        public event Action<WindowGLFW, FocusedChangedEventArgs>? FocusedChanged;

        public event Action<WindowGLFW, CharInputEventArgs>? CharInput;

        public event Action<WindowGLFW, KeyboardKeyEventArgs>? KeyDown;
        public event Action<WindowGLFW, KeyboardKeyEventArgs>? KeyUp;

        public event Action<WindowGLFW, MonitorConnectionEventArgs>? MonitorConnectionChanged;

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

            _frameBufferSizeCallback = (_, width, height) =>
            {
                _frameBufferSize = new Vector2i(width, height);
                FrameBufferSizeChanged?.Invoke(this, _frameBufferSize);
            };
            GLFW.SetFramebufferSizeCallback(_window, _frameBufferSizeCallback);

            _closeCallback = _ =>
            {
                var cancel = false;
                var e = new CancelEventArgs(&cancel);
                Closing?.Invoke(this, e);
                if(e.Cancel) {
                    GLFW.SetWindowShouldClose(_window, false);
                }
            };
            GLFW.SetWindowCloseCallback(_window, _closeCallback);


            _iconifyCallback = (_, minimized) =>
            {
                Minimized?.Invoke(this, new MinimizedEventArgs(minimized));
            };
            GLFW.SetWindowIconifyCallback(_window, _iconifyCallback);

            _focusCallback = (_, focused) =>
            {
                FocusedChanged?.Invoke(this, new FocusedChangedEventArgs(focused));
            };
            GLFW.SetWindowFocusCallback(_window, _focusCallback);

            _charCallback = (_, unicode) =>
            {
                CharInput?.Invoke(this, new CharInputEventArgs(unicode));
            };
            GLFW.SetCharCallback(_window, _charCallback);

            _keyCallback = (_, glfwKey, scanCode, action, glfwMods) =>
            {
                var e = new KeyboardKeyEventArgs(glfwKey, scanCode, glfwMods, action == GlfwInputAction.Repeat);

                if(action == GlfwInputAction.Release) {
                    KeyUp?.Invoke(this, e);
                }
                else {
                    KeyDown?.Invoke(this, e);
                }
            };
            GLFW.SetKeyCallback(_window, _keyCallback);

            _cursorEnterCallback = (_, entered) =>
            {
                if(entered) {
                    MouseEnter?.Invoke(this);
                }
                else {
                    MouseLeave?.Invoke(this);
                }
            };
            GLFW.SetCursorEnterCallback(_window, _cursorEnterCallback);

            _mouseButtonCallback = (_, button, action, mods) =>
            {
                var e = new MouseButtonEventArgs(button, action, mods);

                if(action == GlfwInputAction.Release) {
                    MouseUp?.Invoke(this, e);
                }
                else {
                    MouseDown?.Invoke(this, e);
                }
            };
            GLFW.SetMouseButtonCallback(_window, _mouseButtonCallback);

            _cursorPosCallback = (_, posX, posY) =>
            {
                var e = new MouseMoveEventArgs(new Vector2((int)posX, (int)posY));
                MouseMove?.Invoke(this, e);
            };
            GLFW.SetCursorPosCallback(_window, _cursorPosCallback);

            _scrollCallback = (_, offsetX, offsetY) =>
            {
                var e = new MouseWheelEventArgs((float)offsetX, (float)offsetY);
                MouseWheel?.Invoke(this, e);
            };
            GLFW.SetScrollCallback(_window, _scrollCallback);

            _dropCallback = (_, count, paths) =>
            {
                var e = new FileDropEventArgs(count, paths);
                FileDrop?.Invoke(this, e);
            };
            GLFW.SetDropCallback(_window, _dropCallback);

            _joystickCallback = (joystick, state) =>
            {
                var e = new JoystickConnectionEventArgs(joystick, state == GlfwConnectedState.Connected);
                JoystickConnectionChanged?.Invoke(this, e);
            };
            GLFW.SetJoystickCallback(_joystickCallback);

            _monitorCallback = (monitor, state) =>
            {
                var e = new MonitorConnectionEventArgs(monitor, state == GlfwConnectedState.Connected);
                MonitorConnectionChanged?.Invoke(this, e);
            };
            GLFW.SetMonitorCallback(_monitorCallback);

            _refreshCallback = _ =>
            {
                Refresh?.Invoke(this);
            };
            GLFW.SetWindowRefreshCallback(_window, _refreshCallback);
        }
    }
}
