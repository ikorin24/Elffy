#nullable enable

namespace Elffy.InputSystem
{
    /// <summary>マウスの状態を表すクラスです</summary>
    public class Mouse
    {
        private KeyBuffer _leftButton;
        private KeyBuffer _rightButton;
        private KeyBuffer _middleButton;
        private WheelBuffer _wheel;

        /// <summary>マウスが <see cref="IHostScreen"/> の描画領域内にあるかどうかを取得します</summary>
        public bool OnScreen { get; private set; }

        /// <summary>Top-Left を基点としY軸下方向でのマウスの <see cref="IHostScreen"/> 内での座標を取得します</summary>
        public Vector2 Position { get; private set; }

        internal Mouse() { }

        internal void ChangePosition(in Vector2 position) => Position = position;

        internal void ChangePressedState(MouseButton button, bool isPressed)
        {
            switch(button) {
                case MouseButton.Left:
                    _leftButton.SetValue(isPressed);
                    return;
                case MouseButton.Right:
                    _rightButton.SetValue(isPressed);
                    return;
                case MouseButton.Middle:
                    _middleButton.SetValue(isPressed);
                    return;
                default:
                    return;
            }
        }

        internal void ChangeWheel(float value) => _wheel.SetValue(value);

        /// <summary>現在のマウスが <see cref="IHostScreen"/> の描画領域内にあるかどうかを更新します</summary>
        /// <param name="onScreen">マウスが領域内にあるかどうか</param>
        internal void ChangeOnScreen(bool onScreen) => OnScreen = onScreen;

        internal void InitFrame()
        {
            _leftButton.InitFrame();
            _rightButton.InitFrame();
            _middleButton.InitFrame();
            _wheel.InitFrame();
        }

        public bool IsAnyDown()
        {
            return _leftButton.IsKeyDown() || _rightButton.IsKeyDown() || _middleButton.IsKeyDown();
        }

        public bool IsAnyPressed()
        {
            return _leftButton.IsKeyPressed() || _rightButton.IsKeyPressed() || _middleButton.IsKeyPressed();
        }

        public bool IsAnyUp()
        {
            return _leftButton.IsKeyUp() || _rightButton.IsKeyUp() || _middleButton.IsKeyUp();
        }

        /// <summary>Get whether specified mouse button is donw. (Return true if the button got pressed on this frame.)</summary>
        /// <param name="button">mouse button</param>
        /// <returns>button down state</returns>
        public bool IsDown(MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => _leftButton.IsKeyDown(),
                MouseButton.Right => _rightButton.IsKeyDown(),
                MouseButton.Middle => _middleButton.IsKeyDown(),
                _ => false,
            };
        }

        /// <summary>Get whether specified mouse button is pressed.</summary>
        /// <param name="button">mouse button</param>
        /// <returns>button pressed state</returns>
        public bool IsPressed(MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => _leftButton.IsKeyPressed(),
                MouseButton.Right => _rightButton.IsKeyPressed(),
                MouseButton.Middle => _middleButton.IsKeyPressed(),
                _ => false,
            };
        }

        /// <summary>Get whether specified mouse button Tis up. (Return true if the button got released on this frame.)</summary>
        /// <param name="button">mouse button</param>
        /// <returns>button up state</returns>
        public bool IsUp(MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => _leftButton.IsKeyUp(),
                MouseButton.Right => _rightButton.IsKeyUp(),
                MouseButton.Middle => _middleButton.IsKeyUp(),
                _ => false,
            };
        }

        /// <summary>Get wheel value difference from previouse frame.</summary>
        /// <returns>wheel value</returns>
        public float Wheel() => _wheel.Wheel();

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
            private bool _changed;

            public void SetValue(float value)
            {
                _current = value;
                _changed = true;
            }

            public float Wheel() => _current;

            public void InitFrame()
            {
                if(_changed == false) {
                    _current = 0f;
                }
                _changed = false;
            }
        }
    }
}
