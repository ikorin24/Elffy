#nullable enable
using System;
using System.Drawing;
using Elffy.Effective;

namespace Elffy.Input
{
    /// <summary>マウスの状態を表すクラスです</summary>
    public class Mouse
    {
        private KeyBuffer _leftPressed;
        private KeyBuffer _rightPressed;
        private KeyBuffer _middlePressed;
        private WheelBuffer _wheel;

        /// <summary>マウスが <see cref="IScreenHost"/> の描画領域内にあるかどうかを取得します</summary>
        public bool OnScreen { get; private set; }

        /// <summary>Top-Left を基点とする、マウスの <see cref="IScreenHost"/> 内での座標を取得します</summary>
        public Point Position { get; private set; }

        internal Mouse() { }

        internal void ChangePosition(Point position) => Position = position;

        internal void ChangePressedState(MouseButton button, bool isPressed)
        {
            switch(button) {
                case MouseButton.Left:
                    _leftPressed.SetValue(isPressed);
                    return;
                case MouseButton.Right:
                    _rightPressed.SetValue(isPressed);
                    return;
                case MouseButton.Middle:
                    _middlePressed.SetValue(isPressed);
                    return;
                default:
                    throw UnknownButtonException(button);
            }
        }

        internal void ChangeWheel(float value) => _wheel.SetValue(value);

        /// <summary>現在のマウスが <see cref="IScreenHost"/> の描画領域内にあるかどうかを更新します</summary>
        /// <param name="onScreen">マウスが領域内にあるかどうか</param>
        internal void ChangeOnScreen(bool onScreen) => OnScreen = onScreen;

        internal void InitFrame()
        {
            _leftPressed.InitFrame();
            _rightPressed.InitFrame();
            _middlePressed.InitFrame();
            _wheel.InitFrame();
        }

        /// <summary>Get whether specified mouse button is donw. (Return true if the button got pressed on this frame.)</summary>
        /// <param name="button">mouse button</param>
        /// <returns>button down state</returns>
        public bool IsDown(MouseButton button)
        {
            return (button switch
            {
                MouseButton.Left => _leftPressed,
                MouseButton.Right => _rightPressed,
                MouseButton.Middle => _middlePressed,
                _ => throw UnknownButtonException(button),
            }).IsKeyDown();
        }

        /// <summary>Get whether specified mouse button is pressed.</summary>
        /// <param name="button">mouse button</param>
        /// <returns>button pressed state</returns>
        public bool IsPressed(MouseButton button)
        {
            return (button switch
            {
                MouseButton.Left => _leftPressed,
                MouseButton.Right => _rightPressed,
                MouseButton.Middle => _middlePressed,
                _ => throw UnknownButtonException(button),
            }).IsKeyPressed();
        }

        /// <summary>Get whether specified mouse button Tis up. (Return true if the button got released on this frame.)</summary>
        /// <param name="button">mouse button</param>
        /// <returns>button up state</returns>
        public bool IsUp(MouseButton button)
        {
            return (button switch
            {
                MouseButton.Left => _leftPressed,
                MouseButton.Right => _rightPressed,
                MouseButton.Middle => _middlePressed,
                _ => throw UnknownButtonException(button),
            }).IsKeyUp();
        }

        /// <summary>Get wheel value difference from previouse frame.</summary>
        /// <returns>wheel value</returns>
        public float Wheel() => _wheel.GetDiff();

        private NotSupportedException UnknownButtonException(MouseButton button) 
            => throw new NotSupportedException($"Unknown button type '{button}'".AsInterned());

        private struct KeyBuffer
        {
            private bool _current;
            private bool _prev;
            private bool _changed;

            public void SetValue(bool value)
            {
                _prev = _current;
                _current = value;
                _changed = true;
            }

            public bool IsKeyPressed() => _current;

            public bool IsKeyDown() => _current && !_prev;

            public bool IsKeyUp() => !_current && _prev;

            public void InitFrame()
            {
                if(_changed == false) {
                    _prev = _current;
                }
                _changed = false;
            }
        }

        private struct WheelBuffer
        {
            private float _current;
            private float _prev;
            private bool _changed;

            public void SetValue(float value)
            {
                _prev = _current;
                _current = value;
                _changed = true;
            }

            public float GetDiff()
            {
                return _current - _prev;
            }

            public void InitFrame()
            {
                if(_changed == false) {
                    _prev = _current;
                }
                _changed = false;
            }
        }
    }
}
